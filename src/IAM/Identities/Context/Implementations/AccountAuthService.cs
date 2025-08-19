using Core.Base.Agents.Communication;
using IAM.Identities.Identity;
using IAM.Identities.Service.Implementations.Helpers;
using Microsoft.Extensions.Configuration;
using ServiceKit.Net;

namespace IAM.Identities.Service.Implementations
{
    public class AccountAuthService : IAccountAuthService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IAuthRepository _authRepository;
        private readonly TokenAgent _tokenAgent;
        private readonly EmailAgent _emailAgent;
        private readonly PasswordAgent _passwordAgent;
        private readonly CertificateAgent _certificateAgent;
        private readonly IConfiguration _configuration;
        private readonly ICertificateAuthorityACL _certificateAuthorityACL;

        public AccountAuthService(
            IdentityStoreContext context,
            IAccountRepository accountRepository,
            IAuthRepository authRepository,
            ICertificateAuthorityACL certificateAuthorityACL,
            TokenAgent tokenAgent,
            EmailAgent emailAgent,
            PasswordAgent passwordAgent,
            CertificateAgent certificateAgent,
            IConfiguration configuration)
        {
            _accountRepository = accountRepository;
            _authRepository = authRepository;
            _certificateAuthorityACL = certificateAuthorityACL;
            _tokenAgent = tokenAgent;
            _emailAgent = emailAgent;
            _passwordAgent = passwordAgent;
            _certificateAgent = certificateAgent;
            _configuration = configuration;
        }

        async Task<Response<Auth>> IAccountAuthService.setAuthActive(CallingContext ctx, string accountId, string authId, string etag, bool isActive)
        {
            var account = await _accountRepository.getAccount(ctx, accountId).ConfigureAwait(false);
            if (account.IsFailed())
                return new(account.Error);

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
            var account = await _accountRepository.getAccount(ctx, accountId).ConfigureAwait(false);
            if (account.IsFailed())
                return new(account.Error);

            var already = await _authRepository.findEmailAuthByEmail(ctx, email).ConfigureAwait(false);
            if (already.IsFailed())
                return new(already.Error);
            if (already.HasValue())
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Account authorization is already exist with email: '{email}'", AdditionalInformation = $"Conflicted user: {already.Value.accountId} " });

            var passwordErrors = _passwordAgent.ValidatePasswordRules(password, accountName: email, email: email);
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

            var salt = _passwordAgent.CreateLifetimeSalt();
            var passwordHash = _passwordAgent.GeneratePasswordHash(password, salt);
            if(passwordHash.IsFailed())
                return new(passwordHash.Error);

            var auth = new EmailAuth()
            {
                method = Auth.Methods.Email,
                accountId = accountId,

                email = email,
                isActive = true,
                isEmailConfirmed = false,
                passwordExpiresAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(_passwordAgent.GetExpirationDays()),
                passwordSalt = salt,
                passwordHash = passwordHash.Value,
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

            (string token, DateTime expiresAt) = _tokenAgent.GenerateEmailConfirmationToken(auth.accountId, auth.id);
            await _emailAgent.SendEmailConfirmation(ctx, email, token, expiresAt, _configuration["FrontEnd:EmailConfirmationURL"]);

            return new(auth);
        }

        async Task<Response<EmailAuth>> IAccountAuthService.changePassword(CallingContext ctx, string accountId, string authId, string etag, string oldPasword, string newPassword)
        {
            var account = await _accountRepository.getAccount(ctx, accountId).ConfigureAwait(false);
            if (account.IsFailed())
                return new(account.Error);

            // Loading
            var get = await _authRepository.getEmailAuth(ctx, accountId, authId).ConfigureAwait(false);
            if (get.IsFailed())
                return new(get.Error);

            var auth = get.Value;

            if (string.IsNullOrEmpty(oldPasword) == false)
            {
                bool valid = _passwordAgent.IsPasswordValid(oldPasword, auth.passwordSalt, auth.passwordHash);
                if (valid == false)
                {
                    return new(new Error
                    {
                        Status = Statuses.BadRequest,
                        MessageText = "old password is invalid",
                    });
                }
            }

            var passwordErrors = _passwordAgent.ValidatePasswordRules(newPassword, accountName: auth.email, email: auth.email);
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

            var newPasswordHash = _passwordAgent.GeneratePasswordHash(newPassword, auth.passwordSalt);
            if(newPasswordHash.IsFailed())
                return new(newPasswordHash.Error);

            var current = _passwordAgent.CheckCurrentPassword(newPassword, lifetimeSalt: auth.passwordSalt, currentPasswordHash: auth.passwordHash);
            if (current.IsFailed())
                return new(current.Error);

            var history = _passwordAgent.CheckPasswordHistory(newPassword, lifetimeSalt: auth.passwordSalt, passwordHistory: auth.passwordHistory);
            if (history.IsFailed())
                return new(history.Error);

            _passwordAgent.AppendToHistory(auth.passwordHistory, currentPasswordHash: auth.passwordHash, newPasswordHash: newPasswordHash.Value);

            // Set new hash (salt does NOT change!)
            auth.etag = etag;
            auth.passwordHash = newPasswordHash.Value;
            auth.passwordExpiresAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(_passwordAgent.GetExpirationDays());

            var update = await _authRepository.updateAuth(ctx, auth).ConfigureAwait(false);
            if (update.IsFailed())
                return new(update.Error);

            return new(auth);
        }

        Task<Response<bool>> IAccountAuthService.ForgottPassword(CallingContext ctx, string accountId, string authId, string url)
        {
            throw new NotImplementedException();
        }

        Task<Response<bool>> IAccountAuthService.ResetPassword(CallingContext ctx, string token, string newPassword)
        {
            throw new NotImplementedException();
        }

        async Task<Response<EmailAuth>> IAccountAuthService.setEmailTwoFactor(CallingContext ctx, string accountId, string authId, string etag, bool enabled, TwoFactorConfiguration.Methods method, string phoneNumber, string email)
        {
            var account = await _accountRepository.getAccount(ctx, accountId).ConfigureAwait(false);
            if (account.IsFailed())
                return new(account.Error);

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
            auth.etag = etag;
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
            var tokenData = _tokenAgent.ValidateEmailConfirmationToken(confirmationToken);
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

        async Task<Response<ADAuth>> IAccountAuthService.CreateADAuth(CallingContext ctx, string accountId, string ldapDomainId, string userName, bool enableTwoFactor, TwoFactorConfiguration.Methods twoFactorMethod, string twoFactorPhoneNumber, string twoFactorEmail)
        {
            var account = await _accountRepository.getAccount(ctx, accountId).ConfigureAwait(false);
            if (account.IsFailed())
                return new(account.Error);

            // Uniqueness: (ldapDomainId, userName)
            var exists = await _authRepository.findADAuthByDomainAndUser(ctx, ldapDomainId, userName).ConfigureAwait(false);
            if (exists.IsFailed())
                return new(exists.Error);
            if (exists.HasValue())
                return new(new Error
                {
                    Status = Statuses.BadRequest,
                    MessageText = $"AD auth already exists for domain '{ldapDomainId}' and user '{userName}'.",
                    AdditionalInformation = $"Conflicted authId: {exists.Value.id}, accountId: {exists.Value.accountId}"
                });

            // 2FA validation
            var twoFa = _ValidateTwoFactorInputs(twoFactorMethod, enableTwoFactor, twoFactorPhoneNumber, twoFactorEmail);
            if (twoFa.IsFailed())
                return new(twoFa.Error);

            var auth = new ADAuth
            {
                method = Auth.Methods.ActiveDirectory,
                accountId = accountId,
                isActive = true,

                LdapDomainId = ldapDomainId,
                userName = userName,

                twoFactor = new TwoFactorConfiguration
                {
                    enabled = enableTwoFactor,
                    method = twoFactorMethod,
                    phoneNumber = twoFactorPhoneNumber,
                    email = twoFactorEmail
                }
            };

            var create = await _authRepository.createAuth(ctx, auth).ConfigureAwait(false);
            if (create.IsFailed())
                return new(create.Error);

            return new(auth);
        }

        async Task<Response<ADAuth>> IAccountAuthService.UpdateADAccount(CallingContext ctx, string accountId, string authId, string etag, string ldapDomainId, string userName)
        {
            var account = await _accountRepository.getAccount(ctx, accountId).ConfigureAwait(false);
            if (account.IsFailed())
                return new(account.Error);

            // Load current record
            var get = await _authRepository.getADAuth(ctx, accountId, authId).ConfigureAwait(false);
            if (get.IsFailed())
                return new(get.Error);
            if (!get.HasValue())
                return new(new Error { Status = Statuses.NotFound, MessageText = $"AD auth not found for account '{accountId}', auth '{authId}'." });

            var auth = get.Value;

            // If domain/user changes, enforce uniqueness
            var domainChanged = !string.Equals(auth.LdapDomainId, ldapDomainId, StringComparison.Ordinal);
            var userChanged = !string.Equals(auth.userName, userName, StringComparison.Ordinal);
            if (domainChanged || userChanged)
            {
                var exists = await _authRepository.findADAuthByDomainAndUser(ctx, ldapDomainId, userName).ConfigureAwait(false);
                if (exists.IsFailed())
                    return new(exists.Error);
                if (exists.HasValue() && !string.Equals(exists.Value.id, authId, StringComparison.Ordinal))
                {
                    return new(new Error
                    {
                        Status = Statuses.BadRequest,
                        MessageText = $"Another AD auth already exists for domain '{ldapDomainId}' and user '{userName}'.",
                        AdditionalInformation = $"Conflicted authId: {exists.Value.id}, accountId: {exists.Value.accountId}"
                    });
                }
            }

            auth.etag = etag;
            auth.LdapDomainId = ldapDomainId;
            auth.userName = userName;

            var update = await _authRepository.updateAuth(ctx, auth).ConfigureAwait(false);
            if (update.IsFailed())
                return new(update.Error);

            return new(auth);
        }

        async Task<Response<ADAuth>> IAccountAuthService.SetADTwoFactor(CallingContext ctx, string accountId, string authId, string etag, bool enabled, TwoFactorConfiguration.Methods method, string phoneNumber, string email)
        {
            var account = await _accountRepository.getAccount(ctx, accountId).ConfigureAwait(false);
            if (account.IsFailed())
                return new(account.Error);

            // Load existing EmailAuth
            var get = await _authRepository.getADAuth(ctx, accountId, authId).ConfigureAwait(false);
            if (get.IsFailed())
                return new(get.Error);
            if (!get.HasValue())
                return new(new Error
                {
                    Status = Statuses.NotFound,
                    MessageText = $"Ad auth not found for account '{accountId}' with auth '{authId}'."
                });

            var auth = get.Value;

            var validate = _ValidateTwoFactorInputs(method, enabled, phoneNumber, email);
            if (validate.IsFailed())
                return new(validate.Error);

            // Update twoFactor settings
            auth.etag = etag;
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

        async Task<Response<KAUAuth>> IAccountAuthService.CreateKAUAuth(CallingContext ctx, string accountId, string kauUserId, bool enableTwoFactor, TwoFactorConfiguration.Methods twoFactorMethod, string twoFactorPhoneNumber, string twoFactorEmail)
        {
            var account = await _accountRepository.getAccount(ctx, accountId).ConfigureAwait(false);
            if (account.IsFailed())
                return new(account.Error);

            // Uniqueness by KAU user id
            var exists = await _authRepository.findKAUAuthByUserId(ctx, kauUserId).ConfigureAwait(false);
            if (exists.IsFailed())
                return new(exists.Error);
            if (exists.HasValue())
                return new(new Error
                {
                    Status = Statuses.BadRequest,
                    MessageText = $"KAU auth already exists for KAUUserId '{kauUserId}'.",
                    AdditionalInformation = $"Conflicted authId: {exists.Value.id}, accountId: {exists.Value.accountId}"
                });

            // 2FA validation
            var twoFa = _ValidateTwoFactorInputs(twoFactorMethod, enableTwoFactor, twoFactorPhoneNumber, twoFactorEmail);
            if (twoFa.IsFailed())
                return new(twoFa.Error);

            var auth = new KAUAuth
            {
                method = Auth.Methods.KAU,
                accountId = accountId,
                isActive = true,

                KAUUserId = kauUserId,
                // legalName / email initially empty; can be filled after first successful KAU login or via UpdateKAUProfile
                legalName = null,
                email = null,

                twoFactor = new TwoFactorConfiguration
                {
                    enabled = enableTwoFactor,
                    method = twoFactorMethod,
                    phoneNumber = twoFactorPhoneNumber,
                    email = twoFactorEmail
                }
            };

            var create = await _authRepository.createAuth(ctx, auth).ConfigureAwait(false);
            if (create.IsFailed())
                return new(create.Error);

            return new(auth);
        }

        async Task<Response<KAUAuth>> IAccountAuthService.SetKAUTwoFactor(CallingContext ctx, string accountId, string authId, string etag, bool enabled, TwoFactorConfiguration.Methods method, string phoneNumber, string email)
        {
            var account = await _accountRepository.getAccount(ctx, accountId).ConfigureAwait(false);
            if (account.IsFailed())
                return new(account.Error);

            // Load existing EmailAuth
            var get = await _authRepository.getKAUAuth(ctx, accountId, authId).ConfigureAwait(false);
            if (get.IsFailed())
                return new(get.Error);
            if (!get.HasValue())
                return new(new Error
                {
                    Status = Statuses.NotFound,
                    MessageText = $"KAU not found for account '{accountId}' with auth '{authId}'."
                });

            var auth = get.Value;

            var validate = _ValidateTwoFactorInputs(method, enabled, phoneNumber, email);
            if (validate.IsFailed())
                return new(validate.Error);

            // Update twoFactor settings
            auth.etag = etag;
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

        async Task<Response<CertificateAuth>> IAccountAuthService.CreateCertificateFromCSR(CallingContext ctx, string accountId, string csrPem, string profile)
        {
            var account = await _accountRepository.getAccount(ctx, accountId).ConfigureAwait(false);
            if (account.IsFailed())
                return new(account.Error);

            if (string.IsNullOrWhiteSpace(csrPem))
                return new(new Error { Status = Statuses.BadRequest, MessageText = "CSR PEM must be provided." });

            var signed = await _certificateAgent.SignCsrAndParseAsync(ctx, csrPem, profile).ConfigureAwait(false);
            if (signed.IsFailed())
                return new(signed.Error);

            var certification = signed.Value;

            var auth = new CertificateAuth
            {
                method = Auth.Methods.Certificate,
                accountId = accountId,
                isActive = true,
                
                certificateThumbprint = certification.ThumbprintSha256,
                validFrom = certification.NotBeforeUtc,
                validUntil = certification.NotAfterUtc,

                // ha ezek a mezők léteznek a modelledben, állítsd:
                serialNumber = certification.SerialNumber,
                issuer = certification.Issuer,
                subject = certification.Subject,
                publicKeyHash = certification.SpkiSha256,

                isRevoked = false,
                revocationReason = string.Empty,
                revokedAt = DateTime.MinValue,
            };

            var create = await _authRepository.createAuth(ctx, auth).ConfigureAwait(false);
            if (create.IsFailed())
                return new(create.Error);

            return new(auth);
        }

        async Task<Response<CertificateAuth>> IAccountAuthService.RevokeCertificate(CallingContext ctx, string accountId, string authId, string etag, string reason)
        {
            var account = await _accountRepository.getAccount(ctx, accountId).ConfigureAwait(false);
            if (account.IsFailed())
                return new(account.Error);

            if (string.IsNullOrWhiteSpace(reason))
                return new(new Error { Status = Statuses.BadRequest, MessageText = "Revocation reason is required." });

            var get = await _authRepository.getCertificateAuth(ctx, accountId, authId).ConfigureAwait(false);
            if (get.IsFailed())
                return new(get.Error);
            if (!get.HasValue())
                return new(new Error { Status = Statuses.NotFound, MessageText = $"Certificate auth not found for account '{accountId}', auth '{authId}'." });

            var auth = get.Value;
            if(auth.isRevoked == true)
                return new(new Error { Status = Statuses.BadRequest, MessageText = $"Certificate auth is alerady revoked '{accountId}', auth '{authId}'." });

            // revoke a CA-n
            var rev = await _certificateAgent.RevokeBySerialAsync(ctx, auth.serialNumber, reason).ConfigureAwait(false);
            if (rev.IsFailed())
                return new(rev.Error);

            auth.etag = etag;
            auth.isRevoked = true;
            auth.revocationReason = reason;
            auth.revokedAt = DateTime.UtcNow;

            var upd = await _authRepository.updateAuth(ctx, auth).ConfigureAwait(false);
            if (upd.IsFailed())
                return new(upd.Error);

            return new(auth);
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
