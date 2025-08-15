using Core.Base.Agents.Communication;
using IAM.Identities.Identity;
using IAM.Identities.Service.Implementations.Helpers;
using Microsoft.Extensions.Configuration;
using ServiceKit.Net;
using System.Security.Cryptography;

namespace IAM.Identities.Service.Implementations
{
    public class AccountAuthService : IAccountAuthService
    {
        private readonly IdentityStoreContext _context;
        private readonly IAccountRepository _accountRepository;
        private readonly IAuthRepository _authRepository;
        private readonly TokenService _tokenService;
        private readonly EmailAgent _emailAgent;
        private readonly IConfiguration _configuration;

        public AccountAuthService(
            IdentityStoreContext context,
            IAccountRepository accountRepository,
            IAuthRepository authRepository,
            TokenService tokenService,
            EmailAgent emailAgent,
            IConfiguration configuration)
        {
            _context = context;
            _accountRepository = accountRepository;
            _authRepository = authRepository;
            _tokenService = tokenService;
            _emailAgent = emailAgent;
            _configuration = configuration;
        }

        async Task<Response<Auth>> IAccountAuthService.setAuthActive(CallingContext ctx, string accountId, string authId, string etag, bool isActive)
        {
            var get = await _authRepository.getAuth(ctx, accountId, authId).ConfigureAwait(false);
            if (get.IsFailed())
                return new(get.Error);

            var auth = get.Value;
            auth.etag = etag;
            auth.isActive = isActive;
            var update = await _authRepository.updateAuth(ctx, auth).ConfigureAwait(false);
            if (update.IsFailed())
                return new(update.Error);

            return new(auth);
        }

        async Task<Response<EmailAuth>> IAccountAuthService.createEmailAuth(CallingContext ctx, string accountId, string email, string password, bool enableTwoFactor, TwoFactorConfiguration.Methods twoFactorMethod, string twoFactorPhoneNumber, string twoFactorEmail)
        {
            var already = await _authRepository.findEmailAuthByEmail(ctx, email).ConfigureAwait(false);
            if (already.IsFailed())
                return new(already.Error);
            if (already.HasValue())
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Account authorization is alerady exist with email: '{email}'", AdditionalInformation = $"Conflicted user: {already.Value.accountId} " });

            var passwordErrors = PasswordRules.Validate(password, accountName: email, email: email);
            if (passwordErrors.Count > 0)
            {
                return new(new Error
                {
                    Status = Statuses.BadRequest,
                    MessageText = "Password does not meet policy requirements.",
                    AdditionalInformation = string.Join("; ", passwordErrors)
                });
            }

            // 2FA validation
            var validate = _ValidateTwoFactorInputs(twoFactorMethod, enableTwoFactor, twoFactorPhoneNumber, twoFactorEmail);
            if (validate.IsFailed())
                return new(validate.Error);

            var salt = RandomNumberGenerator.GetBytes(PasswordRules.Salt_Length);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, PasswordRules.Hash_Iterations, HashAlgorithmName.SHA256);
            var passwordHash = pbkdf2.GetBytes(PasswordRules.Hash_KeySize);

            var auth = new EmailAuth()
            {
                method = Auth.Methods.Email,
                accountId = accountId,

                email = email,
                isActive = true,
                isEmailConfirmed = false,
                passwordExpiresAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(PasswordRules.ExpirationDays),
                passwordSalt = Convert.ToBase64String(salt),
                passwordHash = Convert.ToBase64String(passwordHash),
                passwordHistory = [],
                twoFactor = new TwoFactorConfiguration()
                {
                    enabled = enableTwoFactor,
                    email = twoFactorEmail,
                    phoneNumber = twoFactorPhoneNumber,
                    method = twoFactorMethod,
                }
            };

            var result = await _authRepository.createAuth(ctx, auth).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            (string token, DateTime expiresAt) = _tokenService.GenerateEmailConfirmationToken(auth.accountId, auth.id);
            await _emailAgent.SendEmailConfirmation(ctx, email, token, expiresAt, _configuration["FrontEnd:EmailConfirmationURL"]);

            return new(auth);
        }

        async Task<Response<EmailAuth>> IAccountAuthService.changePassword(CallingContext ctx, string accountId, string authId, string etag, string newPassword, DateOnly passwordExpiresAt)
        {
            // Loading
            var get = await _authRepository.getEmailAuth(ctx, accountId, authId).ConfigureAwait(false);
            if (get.IsFailed())
                return new(get.Error);

            var auth = get.Value;


            var passwordErrors = PasswordRules.Validate(newPassword, accountName: auth.email, email: auth.email);
            if (passwordErrors.Count > 0)
            {
                return new(new Error
                {
                    Status = Statuses.BadRequest,
                    MessageText = "Password does not meet policy requirements.",
                    AdditionalInformation = string.Join("; ", passwordErrors)
                });
            }


            // calc hash
            byte[] saltBytes;
            try
            {
                saltBytes = Convert.FromBase64String(auth.passwordSalt);
            }
            catch (FormatException)
            {
                return new(new Error { Status = Statuses.InternalError, MessageText = "Stored password salt is invalid (Base64 decode failed)." });
            }

            using var pbkdf2 = new Rfc2898DeriveBytes(newPassword, saltBytes, PasswordRules.Hash_Iterations, HashAlgorithmName.SHA256);
            byte[] newHashBytes = pbkdf2.GetBytes(PasswordRules.Hash_KeySize);

            // Don't use the same password as your CURRENT password
            try
            {
                var currentHashBytes = Convert.FromBase64String(auth.passwordHash);
                if (CryptographicOperations.FixedTimeEquals(newHashBytes, currentHashBytes))
                {
                    return new(new Error
                    {
                        Status = Statuses.BadRequest,
                        MessageText = "New password must be different from the current password."
                    });
                }
            }
            catch (FormatException)
            {
                return new(new Error { Status = Statuses.InternalError, MessageText = "Stored password hash is invalid (Base64 decode failed)." });
            }

            // Do not use any of your previous passwords
            if (auth.passwordHistory != null && auth.passwordHistory.Count > 0)
            {
                foreach (var prevHashB64 in auth.passwordHistory)
                {
                    if (string.IsNullOrWhiteSpace(prevHashB64))
                        continue;

                    byte[] prevHashBytes;
                    try
                    {
                        prevHashBytes = Convert.FromBase64String(prevHashB64);
                    }
                    catch
                    {
                        // Damaged history: you can consider it a mistake; let's skip it here.
                        continue;
                    }

                    if (CryptographicOperations.FixedTimeEquals(newHashBytes, prevHashBytes))
                    {
                        return new(new Error
                        {
                            Status = Statuses.BadRequest,
                            MessageText = "New password must not match any of the previously used passwords."
                        });
                    }
                }
            }

            // History update (current hash in, FIFO limit optional)
            auth.passwordHistory ??= [];
            if (!string.IsNullOrEmpty(auth.passwordHash))
            {
                auth.passwordHistory.Add(auth.passwordHash);

                if (PasswordRules.History_MaxCount > 0 && auth.passwordHistory.Count > PasswordRules.History_MaxCount)
                {
                    // tartsuk az utolsó N-et
                    var toRemove = auth.passwordHistory.Count - PasswordRules.History_MaxCount;
                    auth.passwordHistory.RemoveRange(0, toRemove);
                }
            }

            // Set new hash (salt does NOT change!)
            auth.passwordHash = Convert.ToBase64String(newHashBytes);
            auth.passwordExpiresAt = passwordExpiresAt;

            var update = await _authRepository.updateAuth(ctx, auth).ConfigureAwait(false);
            if (update.IsFailed())
                return new(update.Error);

            return new(auth);
        }

        async Task<Response<EmailAuth>> IAccountAuthService.setEmailTwoFactor(CallingContext ctx, string accountId, string authId, string etag, bool enabled, TwoFactorConfiguration.Methods method, string phoneNumber, string email)
        {
            // Load existing EmailAuth
            var get = await _authRepository.getEmailAuth(ctx, accountId, authId).ConfigureAwait(false);
            if (get.IsFailed())
                return new(get.Error);
            if (!get.HasValue())
                return new(new Error
                {
                    Status = Statuses.NotFound,
                    MessageText = $"EmailAuth not found for account '{accountId}' with auth '{authId}'."
                });

            var auth = get.Value;

            var validate = _ValidateTwoFactorInputs(method, enabled, phoneNumber, email);
            if (validate.IsFailed())
                return new(validate.Error);

            // Update twoFactor settings
            auth.twoFactor ??= new TwoFactorConfiguration();
            auth.twoFactor.enabled = enabled;
            auth.twoFactor.method = method;
            auth.twoFactor.phoneNumber = phoneNumber;
            auth.twoFactor.email = email;

            var update = await _authRepository.updateAuth(ctx, auth).ConfigureAwait(false);
            if (update.IsFailed())
                return new(update.Error);

            return new(auth);
        }

        async Task<Response<bool>> IAccountAuthService.confirmEmail(CallingContext ctx, string confirmationToken)
        {
            // Validate and decode the confirmation token to extract accountId and authId
            var tokenData = _tokenService.ValidateEmailConfirmationToken(confirmationToken);
            if (tokenData == null)
            {
                return new(new Error
                {
                    Status = Statuses.BadRequest,
                    MessageText = "Invalid or expired email confirmation token."
                });
            }
            var (accountId, authId) = tokenData.Value;

            // Load the EmailAuth record from the repository
            var get = await _authRepository.getEmailAuth(ctx, accountId, authId).ConfigureAwait(false);
            if (get.IsFailed())
                return new(get.Error);

            var auth = get.Value;

            // If email is already confirmed, return success
            if (auth.isEmailConfirmed)
                return new(true);

            // Update the record to mark the email as confirmed
            auth.isEmailConfirmed = true;
            var updateResp = await _authRepository.updateAuth(ctx, auth).ConfigureAwait(false);
            if (updateResp.IsFailed())
                return new(updateResp.Error);

            return new(true);
        }

        Task<Response<ADAuth>> IAccountAuthService.CreateADAuth(CallingContext ctx, string accountId, string ldapDomainId, string userName, bool enableTwoFactor, TwoFactorConfiguration.Methods twoFactorMethod, string twoFactorPhoneNumber, string twoFactorEmail)
        {
            throw new NotImplementedException();
        }

        Task<Response<ADAuth>> IAccountAuthService.UpdateADAccount(CallingContext ctx, string accountId, string authId, string etag, string ldapDomainId, string userName)
        {
            throw new NotImplementedException();
        }

        Task<Response<ADAuth>> IAccountAuthService.SetADTwoFactor(CallingContext ctx, string accountId, string authId, string etag, bool enabled, string method, string phoneNumber, string email)
        {
            throw new NotImplementedException();
        }

        Task<Response<KAUAuth>> IAccountAuthService.CreateKAUAuth(CallingContext ctx, string accountId, string kauUserId, bool enableTwoFactor, TwoFactorConfiguration.Methods twoFactorMethod, string twoFactorPhoneNumber, string twoFactorEmail)
        {
            throw new NotImplementedException();
        }

        Task<Response<KAUAuth>> IAccountAuthService.UpdateKAUProfile(CallingContext ctx, string accountId, string authId, string etag, string legalName, string email)
        {
            throw new NotImplementedException();
        }

        Task<Response<KAUAuth>> IAccountAuthService.SetKAUTwoFactor(CallingContext ctx, string accountId, string authId, string etag, bool enabled, string method, string phoneNumber, string email)
        {
            throw new NotImplementedException();
        }

        Task<Response<CertificateAuth>> IAccountAuthService.CreateCertificateFromCSR(CallingContext ctx, string accountId, string csrPem, string profile)
        {
            throw new NotImplementedException();
        }

        Task<Response<CertificateAuth>> IAccountAuthService.RevokeCertificate(CallingContext ctx, string accountId, string authId, string etag, string reason)
        {
            throw new NotImplementedException();
        }

        private static Response _ValidateTwoFactorInputs(TwoFactorConfiguration.Methods method, bool enabled, string phoneNumber, string email)
        {
            if (!enabled)
                return Response.Success(); // nothing to validate

            switch (method)
            {
                case TwoFactorConfiguration.Methods.SMS:
                    if (string.IsNullOrWhiteSpace(phoneNumber))
                        return Response.Failure(new Error
                        {
                            Status = Statuses.BadRequest,
                            MessageText = "Phone number is required for SMS 2FA."
                        });
                    return Response.Success();

                case TwoFactorConfiguration.Methods.Email:
                    if (string.IsNullOrWhiteSpace(email))
                        return Response.Failure(new Error
                        {
                            Status = Statuses.BadRequest,
                            MessageText = "Email address is required for Email 2FA."
                        });
                    try
                    {
                        _ = new System.Net.Mail.MailAddress(email);
                    }
                    catch
                    {
                        return Response.Failure(new Error
                        {
                            Status = Statuses.BadRequest,
                            MessageText = "Email address for 2FA is invalid."
                        });
                    }
                    return Response.Success();

                case TwoFactorConfiguration.Methods.TOTP:
                    // nothing to validate
                    return Response.Success();

                default:
                    return Response.Failure(new Error
                    {
                        Status = Statuses.InternalError,
                        MessageText = "Unknown two-factor method."
                    });
            }
        }
    }
}
