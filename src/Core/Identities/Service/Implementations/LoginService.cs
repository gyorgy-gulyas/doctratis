using Core.Base;
using Core.Identities.Identity;
using Core.Identities.Service.Implementations.Helpers;
using OtpNet;
using ServiceKit.Net;
using System.Security.Cryptography;

namespace Core.Identities.Service.Implementations
{
    public class LoginService : ILoginService
    {
        private readonly IdentityStoreContext _context;
        private readonly IAccountService _accountService;
        private readonly ISmsService _smsService;
        private readonly IEmailService _emailService;
        private readonly TokenService _tokenService = new();
        private readonly LdapAuthenticator _ldapAuthenticator;
        private readonly KAUAuthenticator _kauAuthenticator;

        public LoginService(IdentityStoreContext context
            , IAccountService accountService
            , ISmsService smsService
            , IEmailService emailService
            , LdapAuthenticator ldapAuthenticator
            , KAUAuthenticator kauAuthenticator)
        {
            _context = context;
            _accountService = accountService;
            _smsService = smsService;
            _emailService = emailService;
            _ldapAuthenticator = ldapAuthenticator;
            _kauAuthenticator = kauAuthenticator;
        }

        async Task<Response<ILoginIF_v1.LoginResultDTO>> ILoginService.LoginWithEmailPassword(CallingContext ctx, string email, string password)
        {
            var result = await _accountService.findUserByEmail(ctx, email).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            // if the user not found, do not tell on api, just InvalidUserNameOrPassword, to avoid the guess the username
            if (result.HasValue() == false)
                return new(new ILoginIF_v1.LoginResultDTO() { result = ILoginIF_v1.SignInResult.InvalidUserNameOrPassword });

            var account = result.Value;

            var signIn = _trySignInWithPassword(account, password);
            if (signIn != ILoginIF_v1.SignInResult.Ok)
            {
                _context.AuditLog_SignInFailed(ctx, account, account.emailAndPasswordAuth, signIn);
                return new(new ILoginIF_v1.LoginResultDTO() { result = signIn });
            }

            return await _HandleSuccessSignIn(ctx, account, account.emailAndPasswordAuth);
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

            var result = await _accountService.findUserByADCredentrials(ctx, domain, userName).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);
            if (result.HasValue() == false)
                return new(new ILoginIF_v1.LoginResultDTO() { result = ILoginIF_v1.SignInResult.DomainUserNotRegistered });

            var account = result.Value;

            return await _HandleSuccessSignIn(ctx, account, account.adAuth);
        }

        Task<Response<string>> ILoginService.GetKAULoginURL(CallingContext ctx, string redirectUrl, string backendCallbackUrl)
        {
            var state = _kauAuthenticator.GenerateUniqueState(redirectUrl);

            var loginUrl = _kauAuthenticator.GetLoginUrl(state, backendCallbackUrl);
        }

        Task<Response<string>> ILoginService.KAUCallback(CallingContext ctx, string code, string state)
        {
            if (!_kauAuthenticator.ValidateState(state, out var returnUrl))
                return Unauthorized("Invalid or expired state");
        }


        private async Task<Response<ILoginIF_v1.LoginResultDTO>> _HandleSuccessSignIn(CallingContext ctx, Account account, Auth auth)
        {
            if (account.twoFactor?.enabled == true)
            {
                return await _Generate2FaTokensAndSendCode(ctx, account);
            }
            else
            {
                _context.AuditLog_LoggedIn(ctx, account, auth);

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


            if (account.twoFactor == null || account.twoFactor.enabled == false)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"User with account id:'{ctx.IdentityId}:{ctx.IdentityName}' does not have two factor auth enabled" });

            code = code.Trim().Replace(" ", "");

            var secretBytes = Base32Encoding.ToBytes(account.twoFactor.totpSecret);

            switch (account.twoFactor.method)
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
                _context.AuditLog_2FAFailed(ctx, account, account.twoFactor.method);
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

        private async Task<Response<ILoginIF_v1.LoginResultDTO>> _Generate2FaTokensAndSendCode(CallingContext ctx, Account account)
        {
            // Access token for 2FA verification (valid only for 5 minutes)
            var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(
                userId: account.id,
                userName: account.Name,
                roles: ["User"],
                customTokenValidityInMinutes: 5
            );

            // Send 2FA code if needed
            await SendTwoFactorCode(account.twoFactor);

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
            async Task SendTwoFactorCode(TwoFactorConfiguration twoFactor)
            {
                var secretBytes = Base32Encoding.ToBytes(twoFactor.totpSecret);

                string code;

                switch (twoFactor.method)
                {
                    case TwoFactorConfiguration.Method.SMS:
                        {
                            var totp = new Totp(secretBytes, step: 5 * 30); // 5 minutes window
                            await _smsService.SendOTP(ctx, twoFactor.phoneNumber, code = totp.ComputeTotp());
                            _context.AuditLog_2FASent(ctx, account, twoFactor.phoneNumber);
                        }
                        break;
                    case TwoFactorConfiguration.Method.Email:
                        {
                            var totp = new Totp(secretBytes, step: 5 * 30); // 5 minutes window
                            await _emailService.SendOTP(ctx, twoFactor.email, code = totp.ComputeTotp());
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
        private ILoginIF_v1.SignInResult _trySignInWithPassword(Account account, string password)
        {
            // If the account doesn't support email + password authentication,
            // return a generic InvalidUserNameOrPassword to avoid leaking username validity
            if (account.emailAndPasswordAuth == null)
                return ILoginIF_v1.SignInResult.InvalidUserNameOrPassword;

            // If the account is not active, explicitly return UserIsNotActive
            if (!account.isActive)
                return ILoginIF_v1.SignInResult.UserIsNotActive;

            // If the email is not confirmed, explicitly return EmailNotConfirmed
            if (!account.emailAndPasswordAuth.isEmailConfirmed)
                return ILoginIF_v1.SignInResult.EmailNotConfirmed;

            // If the password has expired, explicitly return PasswordExpired
            if (account.emailAndPasswordAuth.passwordExpiresAt < DateOnly.FromDateTime(DateTime.UtcNow))
                return ILoginIF_v1.SignInResult.PasswordExpired;

            // If password does not match, return InvalidUserNameOrPassword
            // to avoid leaking information about valid usernames
            if (!IsPasswordValid(password, account.emailAndPasswordAuth))
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
