using Core.Base.Agents.Communication;
using IAM.Identities.Identity;
using IAM.Identities.Service.Implementations.Helpers;
using OtpNet;
using ServiceKit.Net;
using System.Security.Claims;
using System.Security.Cryptography;

namespace IAM.Identities.Service.Implementations
{
    public class LoginService : ILoginService
    {
        private readonly IdentityStoreContext _context;
        private readonly IAccountService _accountService;
        private readonly SmsAgent _smsAgent;
        private readonly EmailAgent _emailAgent;
        private readonly TokenService _tokenService = new();
        private readonly LdapAuthenticator _ldapAuthenticator;
        private readonly KAUAuthenticator _kauAuthenticator;

        public LoginService(IdentityStoreContext context
            , IAccountService accountService
            , SmsAgent smsAgent
            , EmailAgent emailAgent
            , LdapAuthenticator ldapAuthenticator
            , KAUAuthenticator kauAuthenticator)
        {
            _context = context;
            _accountService = accountService;
            _smsAgent = smsAgent;
            _emailAgent = emailAgent;
            _ldapAuthenticator = ldapAuthenticator;
            _kauAuthenticator = kauAuthenticator;
        }

        async Task<Response<ILoginIF_v1.LoginResultDTO>> ILoginService.LoginWithEmailPassword(CallingContext ctx, string email, string password)
        {
            var result = await _accountService.findAccountByEmailAuth(ctx, email).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            // if the user not found, do not tell on api, just InvalidUserNameOrPassword, to avoid the guess the username
            if (result.HasValue() == false)
                return new(new ILoginIF_v1.LoginResultDTO() { result = ILoginIF_v1.SignInResult.InvalidUserNameOrPassword });

            var account = result.Value.account;
            var auth = result.Value.auth as EmailAndPasswordAuth;

            var signIn = _trySignInWithPassword(account, auth as EmailAndPasswordAuth, password);
            if (signIn != ILoginIF_v1.SignInResult.Ok)
            {
                _context.AuditLog_SignInFailed(ctx, account, Auth.Methods.Email, signIn);
                return new(new ILoginIF_v1.LoginResultDTO() { result = signIn });
            }

            return await _HandleSuccessSignIn(ctx, account, Auth.Methods.Email, auth.twoFactor);
        }

        async Task<Response<ILoginIF_v1.LoginResultDTO>> ILoginService.LoginWithAD(CallingContext ctx, string username, string password)
        {
            var (domainName, userName) = _ldapAuthenticator.SplitDomainAndUser(username);
            if (string.IsNullOrEmpty(domainName) == true)
                return new(new ILoginIF_v1.LoginResultDTO() { result = ILoginIF_v1.SignInResult.DomainNotSpecified });

            var domain = _ldapAuthenticator.findDomain(domainName);
            if (domain == null)
                return new(new ILoginIF_v1.LoginResultDTO() { result = ILoginIF_v1.SignInResult.DomainNotRegistered });

            var sucess = await _ldapAuthenticator.AuthenticateUser(domain, userName, password);
            if (sucess == false)
                return new(new ILoginIF_v1.LoginResultDTO() { result = ILoginIF_v1.SignInResult.InvalidUserNameOrPassword });

            var result = await _accountService.findAccountByADCredentrials(ctx, domain, userName).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);
            if (result.HasValue() == false)
                return new(new ILoginIF_v1.LoginResultDTO() { result = ILoginIF_v1.SignInResult.DomainUserNotRegistered });

            var account = result.Value.account;
            var auth = result.Value.auth as ADAuth;

            return await _HandleSuccessSignIn(ctx, account, Auth.Methods.ActiveDirectory, auth.twoFactor);
        }

        Task<Response<string>> ILoginService.GetKAULoginURL(CallingContext ctx, string redirectUrl, string backendCallbackUrl)
        {
            var state = _kauAuthenticator.GenerateUniqueState(redirectUrl);

            var loginUrl = _kauAuthenticator.GetLoginUrl(state, backendCallbackUrl);

            return Response<string>.Success(loginUrl).AsTask();
        }

        async Task<Response<ILoginService.KAUCallbackResponse>> ILoginService.KAUCallback(CallingContext ctx, string code, string state)
        {
            if (!_kauAuthenticator.ValidateState(state, out var returnUrl))
                return Unauthorized("Invalid or expired state");

            var tokenResponse = await _kauAuthenticator.ExchangeCodeForToken(code, "https://backend.hu/callback");
            if (tokenResponse?.id_token == null)
                return TokenError(returnUrl);

            var kauUserInfo = _kauAuthenticator.ParseToken(tokenResponse.id_token);
            if (kauUserInfo == null)
                return TokenError(returnUrl);

            var result = await _accountService.findAccountKAUUserId(ctx, kauUserInfo.UserId);
            if (!result.HasValue())
                return UserNotFound(returnUrl);

            var account = result.Value.account;
            var auth = result.Value.auth as KAUAuth;

            _context.AuditLog_LoggedIn(ctx, account, Auth.Methods.KAU);

            var tokens = _Login(ctx, account);
            return Success(returnUrl, tokens, auth.twoFactor?.enabled ?? false);

            // Lokális segédfüggvények a válaszok egyszerűsítésére:
            static Response<ILoginService.KAUCallbackResponse> Unauthorized(string message) =>
                new(new Error { Status = Statuses.Unauthorized, MessageText = message });

            static Response<ILoginService.KAUCallbackResponse> TokenError(string returnUrl) =>
                new(new ILoginService.KAUCallbackResponse
                {
                    returnUrl = returnUrl,
                    result = new ILoginIF_v1.LoginResultDTO
                    {
                        result = ILoginIF_v1.SignInResult.KAUTokenError,
                        requires2FA = false,
                        tokens = null
                    }
                });

            static Response<ILoginService.KAUCallbackResponse> UserNotFound(string returnUrl) =>
                new(new ILoginService.KAUCallbackResponse
                {
                    returnUrl = returnUrl,
                    result = new ILoginIF_v1.LoginResultDTO
                    {
                        result = ILoginIF_v1.SignInResult.KAUUserNotFound,
                        requires2FA = false,
                        tokens = null
                    }
                });

            static Response<ILoginService.KAUCallbackResponse> Success(string returnUrl, ILoginIF_v1.TokensDTO tokens, bool requires2FA) =>
                new(new ILoginService.KAUCallbackResponse
                {
                    returnUrl = returnUrl,
                    result = new ILoginIF_v1.LoginResultDTO
                    {
                        result = ILoginIF_v1.SignInResult.Ok,
                        requires2FA = requires2FA,
                        tokens = tokens
                    }
                });
        }

        private async Task<Response<ILoginIF_v1.LoginResultDTO>> _HandleSuccessSignIn(CallingContext ctx, Account account, Auth.Methods authMethod, TwoFactorConfiguration twoFactor )
        {
            if (twoFactor?.enabled == true)
            {
                return await _Generate2FaTokensAndSendCode(ctx, account, twoFactor);
            }
            else
            {
                _context.AuditLog_LoggedIn(ctx, account, authMethod);

                var tokens = _Login(ctx, account);
                return new(new ILoginIF_v1.LoginResultDTO()
                {
                    requires2FA = false,
                    result = ILoginIF_v1.SignInResult.Ok,
                    tokens = tokens
                });
            }
        }

        private ILoginIF_v1.TokensDTO _Login(CallingContext ctx, Account account)
        {
            // Access token generate
            var (accessToken, accessTokenExpiresAt) = _tokenService.GenerateAccessToken(
                userId: account.id,
                userName: account.Name,
                roles: ["User"]
            );

            // refresh token generate
            var (refreshToken, refreshTokenExpiresAt) = _tokenService.GenerateRefreshToken(account.id);

            return new ILoginIF_v1.TokensDTO()
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshTokenExpiresAt
            };
        }

        async Task<Response<ILoginIF_v1.TokensDTO>> ILoginService.Login2FA(CallingContext ctx, string code)
        {
            var account = await _context.Accounts.Find(ctx.IdentityId, ctx.IdentityId);
            if (account == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"User with account id:'{ctx.IdentityId}:{ctx.IdentityName}' not found" });

            if( ctx.Claims.TryGetValue("2faMethod", out var methodClaim) == false )
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"'2faMethod' not found in claims" });

            if (Enum.TryParse<TwoFactorConfiguration.Method>(methodClaim, out var method) == false)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Unknown 2fa method: '{method}'" });

            var secretBytes = Base32Encoding.ToBytes(account.accountSecret);
            code = code.Trim().Replace(" ", "");

            switch (method)
            {
                case TwoFactorConfiguration.Method.SMS:
                case TwoFactorConfiguration.Method.Email:
                    {
                        var totp = new Totp(secretBytes, step: 5 * 30); // 5 perces ablak
                        if (!totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1)))
                            return Fail2FA();
                    }
                    break;

                case TwoFactorConfiguration.Method.TOTP:
                    {
                        var totp = new Totp(secretBytes, step: 30); // 30 másodperces ablak
                        if (!totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1)))
                            return Fail2FA();
                    }
                    break;
            }

            _context.AuditLog_2FASuccess(ctx, account);

            return new(_Login(ctx, account));

            Response<ILoginIF_v1.TokensDTO> Fail2FA()
            {
                _context.AuditLog_2FAFailed(ctx, account, method);
                return new(new Error() { Status = Statuses.Unauthorized, MessageText = $"Invalid TOTP code" });
            }
        }

        async Task<Response<ILoginIF_v1.TokensDTO>> ILoginService.RefreshTokens(CallingContext ctx, string refreshToken)
        {
            var userId = _tokenService.ValidateRefreshToken(refreshToken);
            if (userId != ctx.IdentityId)
                return new(new Error() { Status = Statuses.Unauthorized, MessageText = $"invalid token" });

            var account = await _context.Accounts.Find(ctx.IdentityId, ctx.IdentityId);
            if (account == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"User with account id:'{ctx.IdentityId}:{ctx.IdentityName}' not found" });

            _context.AuditLog_TokenRefreshed(ctx, account);

            return new(_Login(ctx, account));
        }

        private async Task<Response<ILoginIF_v1.LoginResultDTO>> _Generate2FaTokensAndSendCode(CallingContext ctx, Account account, TwoFactorConfiguration twoFactor)
        {
            // Access token for 2FA verification (valid only for 5 minutes)
            var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(
                userId: account.id,
                userName: account.Name,
                roles: ["User"],
                additioinalClaims: new List<Claim>() {
                    new Claim("2faMethod", twoFactor.method.ToString()),
                },
                customTokenValidityInMinutes: 5
            );

            // Send 2FA code if needed
            await SendTwoFactorCode(account.accountSecret);

            // Build and return the response
            return new(new ILoginIF_v1.LoginResultDTO
            {
                requires2FA = true,
                result = ILoginIF_v1.SignInResult.Ok,
                tokens = new ILoginIF_v1.TokensDTO
                {
                    AccessToken = accessToken,
                    AccessTokenExpiresAt = expiresAt,
                    RefreshToken = null,
                    RefreshTokenExpiresAt = DateTime.MinValue
                }
            });

            // --- Local helper function ---
            async Task SendTwoFactorCode(string secret)
            {
                var secretBytes = Base32Encoding.ToBytes(secret);

                string code;

                switch (twoFactor.method)
                {
                    case TwoFactorConfiguration.Method.SMS:
                        {
                            var totp = new Totp(secretBytes, step: 5 * 30); // 5 minutes window
                            await _smsAgent.SendOTP(ctx, twoFactor.phoneNumber, code = totp.ComputeTotp());
                            _context.AuditLog_2FASent(ctx, account, twoFactor.phoneNumber);
                        }
                        break;
                    case TwoFactorConfiguration.Method.Email:
                        {
                            var totp = new Totp(secretBytes, step: 5 * 30); // 5 minutes window
                            await _emailAgent.SendOTP(ctx, twoFactor.email, code = totp.ComputeTotp());
                            _context.AuditLog_2FASent(ctx, account, twoFactor.email);
                        }
                        break;
                    case TwoFactorConfiguration.Method.TOTP:
                        // No code needs to be sent; user already has the TOTP app
                        break;
                }
            }
        }

        /// Attempts to sign in with the given account and password
        /// Returns a SignInResult indicating success or failure reason
        private ILoginIF_v1.SignInResult _trySignInWithPassword(Account account, EmailAndPasswordAuth auth, string password)
        {
            // If the account is not active, explicitly return UserIsNotActive
            if (!account.isActive)
                return ILoginIF_v1.SignInResult.UserIsNotActive;

            // If the email is not confirmed, explicitly return EmailNotConfirmed
            if (!auth.isEmailConfirmed)
                return ILoginIF_v1.SignInResult.EmailNotConfirmed;

            // If the password has expired, explicitly return PasswordExpired
            if (auth.passwordExpiresAt < DateOnly.FromDateTime(DateTime.UtcNow))
                return ILoginIF_v1.SignInResult.PasswordExpired;

            // If password does not match, return InvalidUserNameOrPassword
            // to avoid leaking information about valid usernames
            if (!IsPasswordValid(password, auth))
                return ILoginIF_v1.SignInResult.InvalidUserNameOrPassword;

            // Successful sign-in
            return ILoginIF_v1.SignInResult.Ok;

            // --- Local helper functions ---
            bool IsPasswordValid(string password, EmailAndPasswordAuth emailAuth)
            {
                var saltBytes = Convert.FromBase64String(emailAuth.passwordSalt);
                var hashBytes = Convert.FromBase64String(emailAuth.passwordHash);

                using var pbkdf2 = new Rfc2898DeriveBytes(
                    password,
                    saltBytes,
                    PasswordRules.Hash_Iterations,
                    HashAlgorithmName.SHA256);

                var computedHash = pbkdf2.GetBytes(PasswordRules.Hash_KeySize);

                // Constant-time comparison to prevent timing attacks
                return CryptographicOperations.FixedTimeEquals(computedHash, hashBytes);
            }
        }
    }
}
