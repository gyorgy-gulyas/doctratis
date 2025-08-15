using IAM.Identities.Identity;
using IAM.Identities.Ldap;
using ServiceKit.Net;

namespace IAM.Identities.Service.Implementations
{
    public class IdentityAdminIF_v1 : IIdentityAdminIF_v1
    {
        private readonly IAccountService _accountService;
        private readonly IAccountRepository _accountRepository;
        private readonly IAuthRepository _authRepository;
        private readonly ILdapDomainRepository _ldapDomainRepository;

        public IdentityAdminIF_v1(
            IAccountService accountService,
            IAccountRepository accountRepository,
            IAuthRepository authRepository,
            ILdapDomainRepository ldapDomainRepository)
        {
            _accountService = accountService;
            _accountRepository = accountRepository;
            _authRepository = authRepository;
            _ldapDomainRepository = ldapDomainRepository;
        }

        async Task<Response<IIdentityAdminIF_v1.LdapDomainDTO>> IIdentityAdminIF_v1.RegisterLdapDomain(CallingContext ctx, IIdentityAdminIF_v1.LdapDomainDTO ldap)
        {
            var domain = ldap.Convert();

            var result = await _ldapDomainRepository.insertLdapDomain(ctx, domain).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            return new(result.Value.Convert());
        }

        async Task<Response<IIdentityAdminIF_v1.LdapDomainDTO>> IIdentityAdminIF_v1.UpdateRegisteredLdapDomain(CallingContext ctx, IIdentityAdminIF_v1.LdapDomainDTO ldap)
        {
            var domain = ldap.Convert();

            var result = await _ldapDomainRepository.updateLdapDomain(ctx, domain).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            return new(result.Value.Convert());
        }

        async Task<Response<IIdentityAdminIF_v1.LdapDomainDTO>> IIdentityAdminIF_v1.GetRegisteredLdapDomain(CallingContext ctx, string id)
        {
            var result = await _ldapDomainRepository.getLdapDomain(ctx, id).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            return new(result.Value.Convert());
        }

        async Task<Response<List<IIdentityAdminIF_v1.LdapDomainSummaryDTO>>> IIdentityAdminIF_v1.GetAllRegisteredLdapDomain(CallingContext ctx)
        {
            var result = await _ldapDomainRepository.getAllLdapDomain(ctx).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            return new(result.Value.Select(d => d.ConvertToSummary()).ToList());
        }

        async Task<Response<IIdentityAdminIF_v1.AccountDTO>> IIdentityAdminIF_v1.getAccount(CallingContext ctx, string id)
        {
            var result = await _accountRepository.getAccount(ctx, id).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            return new(result.Value.Convert());
        }

        async Task<Response<List<IIdentityAdminIF_v1.AccountSummaryDTO>>> IIdentityAdminIF_v1.getAllAccount(CallingContext ctx)
        {
            var result = await _accountRepository.getAllAccount(ctx).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            return new(result.Value.Select(a => a.ConvertToSummary()).ToList());
        }

        async Task<Response<IIdentityAdminIF_v1.AccountDTO>> IIdentityAdminIF_v1.createAccount(CallingContext ctx, string username, IIdentityAdminIF_v1.AccountTypes accountType)
        {
            var data = new IAccountService.AccountData()
            {
                Name = username,
                isActive = true,
                contacts = [],
                Type = accountType.Convert()
            };
            var result = await _accountService.createAccount(ctx, data);
            if (result.IsFailed())
                return new(result.Error);

            return new(result.Value.Convert());
        }

        async Task<Response<IIdentityAdminIF_v1.AccountDTO>> IIdentityAdminIF_v1.updateAccount(CallingContext ctx, string accountId, string etag, IIdentityAdminIF_v1.AccountDataDTO dto)
        {
            var data = dto.Convert();
            var result = await _accountService.updateAccount(ctx, accountId, etag, data);
            if (result.IsFailed())
                return new(result.Error);

            return new(result.Value.Convert());
        }

        async Task<Response<List<IIdentityAdminIF_v1.AuthDTO>>> IIdentityAdminIF_v1.listAuthsForAccount(CallingContext ctx, string accountId)
        {
            var auths = await _authRepository.listAuthsForAccount(ctx, accountId);

            return new(auths.Value.Select(ah => ah.Convert()).ToList());
        }

        Task<Response<IIdentityAdminIF_v1.AuthDTO>> IIdentityAdminIF_v1.setActiveForAuth(CallingContext ctx, string accountId, string authId, bool isActive)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.EmailAuthDTO>> IIdentityAdminIF_v1.createtEmailAuth(CallingContext ctx, string accountId, string email, bool initialPassword, IIdentityAdminIF_v1.TwoFactorConfigurationDTO twoFactor)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.EmailAuthDTO>> IIdentityAdminIF_v1.getEmailAuth(CallingContext ctx, string accountId, string authId)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.EmailAuthDTO>> IIdentityAdminIF_v1.changePasswordOnEmailAuth(CallingContext ctx, string accountId, string authId, string etag, string newPassword)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.EmailAuthDTO>> IIdentityAdminIF_v1.setTwoFactorOnEmailAuth(CallingContext ctx, string accountId, string authId, string etag, IIdentityAdminIF_v1.TwoFactorConfigurationDTO twoFactor)
        {
            throw new NotImplementedException();
        }

        Task<Response<bool>> IIdentityAdminIF_v1.confirmEmail(CallingContext ctx, string token)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.ADAuthDTO>> IIdentityAdminIF_v1.createADAuth(CallingContext ctx, string accountId, string ldapDomainId, string adUsername, IIdentityAdminIF_v1.TwoFactorConfigurationDTO twoFactor)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.ADAuthDTO>> IIdentityAdminIF_v1.getADAuth(CallingContext ctx, string accountId, string authId)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.ADAuthDTO>> IIdentityAdminIF_v1.setTwoFactorOnADAuth(CallingContext ctx, string accountId, string authId, string etag, IIdentityAdminIF_v1.TwoFactorConfigurationDTO twoFactor)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.KAUAuthDTO>> IIdentityAdminIF_v1.createKAUAuth(CallingContext ctx, string accountId, string kauUserId, IIdentityAdminIF_v1.TwoFactorConfigurationDTO twoFactor)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.KAUAuthDTO>> IIdentityAdminIF_v1.getKAUAuth(CallingContext ctx, string accountId, string authId)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.KAUAuthDTO>> IIdentityAdminIF_v1.setTwoFactorOnKAUAuth(CallingContext ctx, string accountId, string authId, string etag, IIdentityAdminIF_v1.TwoFactorConfigurationDTO twoFactor)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.CertificateAuthDTO>> IIdentityAdminIF_v1.createCertificateAuthFromCSR(CallingContext ctx, string accountId, IIdentityAdminIF_v1.CsrInputDTO data)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.CertificateAuthDTO>> IIdentityAdminIF_v1.setCertificateAuthActive(CallingContext ctx, string accountId, string authId, string etag, bool isActive)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.CertificateAuthDTO>> IIdentityAdminIF_v1.revokeCertificate(CallingContext ctx, string accountId, string authId, string etag, string reason)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.CertificateAuthDTO>> IIdentityAdminIF_v1.reissueCertificate(CallingContext ctx, string accountId, string authId, IIdentityAdminIF_v1.CsrInputDTO data)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.CertificateAuthDTO>> IIdentityAdminIF_v1.getCertificateAuth(CallingContext ctx, string accountId, string authId)
        {
            throw new NotImplementedException();
        }
    }

    internal static class ConversionExtensions
    {
        internal static Ldap.LdapDomain Convert(this IIdentityAdminIF_v1.LdapDomainDTO @this)
        {
            return new()
            {
                id = @this.id,
                etag = @this.etag,
                LastUpdate = @this.LastUpdate,
                name = @this.name,
                description = @this.description,
                netbiosName = @this.netbiosName,
                domainControllers = @this.domainControllers.Select(dc => new Ldap.LdapDomain.DomainController()
                {
                    host = dc.host,
                    port = dc.port
                }).ToList(),
                baseDn = @this.baseDn,
                useSecureLdap = @this.useSecureLdap,
                serviceAccountUser = @this.serviceAccountUser,
                serviceAccountPassword = @this.serviceAccountPassword,
            };
        }

        internal static IIdentityAdminIF_v1.LdapDomainDTO Convert(this Ldap.LdapDomain @this)
        {
            return new()
            {
                id = @this.id,
                etag = @this.etag,
                LastUpdate = @this.LastUpdate,
                name = @this.name,
                description = @this.description,
                netbiosName = @this.netbiosName,
                domainControllers = @this.domainControllers.Select(dc => new IIdentityAdminIF_v1.LdapDomainDTO.DomainController()
                {
                    host = dc.host,
                    port = dc.port
                }).ToList(),
                baseDn = @this.baseDn,
                useSecureLdap = @this.useSecureLdap,
                serviceAccountUser = @this.serviceAccountUser,
                serviceAccountPassword = @this.serviceAccountPassword,
            };
        }

        internal static IIdentityAdminIF_v1.LdapDomainSummaryDTO ConvertToSummary(this Ldap.LdapDomain @this)
        {
            return new()
            {
                id = @this.id,
                name = @this.name,
                description = @this.description,
            };
        }

        internal static IIdentityAdminIF_v1.AccountDTO Convert(this Account @this)
        {
            return new()
            {
                id = @this.id,
                etag = @this.etag,
                LastUpdate = @this.LastUpdate,
                data = new()
                {
                    Name = @this.Name,
                    isActive = @this.isActive,
                    contacts = @this.contacts.Select(c => c.Convert()).ToList(),
                    Type = @this.Type.Convert(),
                }
            };
        }

        internal static IAccountService.AccountData Convert(this IIdentityAdminIF_v1.AccountDataDTO @this)
        {
            return new()
            {
                Name = @this.Name,
                isActive = @this.isActive,
                contacts = @this.contacts.Select(c => c.Convert()).ToList(),
                Type = @this.Type.Convert(),
            };
        }

        internal static IIdentityAdminIF_v1.AccountSummaryDTO ConvertToSummary(this Account @this)
        {
            return new()
            {
                id = @this.id,
                Name = @this.Name,
                isActive = @this.isActive,
                Type = @this.Type.Convert(),
            };
        }

        internal static ContactInfo Convert(this IIdentityAdminIF_v1.ContactInfo @this)
        {
            return new()
            {
                contactType = @this.contactType,
                email = @this.email,
                phoneNumber = @this.phoneNumber,
            };
        }

        internal static IIdentityAdminIF_v1.ContactInfo Convert(this ContactInfo @this)
        {
            return new()
            {
                contactType = @this.contactType,
                email = @this.email,
                phoneNumber = @this.phoneNumber,
            };
        }

        internal static Account.Types Convert(this IIdentityAdminIF_v1.AccountTypes @this)
        {
            return @this switch
            {
                IIdentityAdminIF_v1.AccountTypes.User => Account.Types.User,
                IIdentityAdminIF_v1.AccountTypes.ExternalSystem => Account.Types.ExternalSystem,
                IIdentityAdminIF_v1.AccountTypes.InternalService => Account.Types.InternalService,
                _ => throw new NotImplementedException(),
            };
        }

        internal static IIdentityAdminIF_v1.AccountTypes Convert(this Account.Types @this)
        {
            return @this switch
            {
                Account.Types.User => IIdentityAdminIF_v1.AccountTypes.User,
                Account.Types.ExternalSystem => IIdentityAdminIF_v1.AccountTypes.ExternalSystem,
                Account.Types.InternalService => IIdentityAdminIF_v1.AccountTypes.InternalService,
                _ => throw new NotImplementedException(),
            };
        }

        internal static IIdentityAdminIF_v1.AuthDTO Convert(this Auth @this)
        {
            return new IIdentityAdminIF_v1.AuthDTO()
            {
                id = @this.id,
                isActive = @this.isActive,
                method = @this.method.Convert(),
            };
        }

        internal static IIdentityAdminIF_v1.EmailAuthDTO Convert(this EmailAuth @this)
        {
            return new IIdentityAdminIF_v1.EmailAuthDTO()
            {
                id = @this.id,
                etag = @this.etag,
                LastUpdate = @this.LastUpdate,
                isActive = @this.isActive,

                email = @this.email,
                isEmailConfirmed = @this.isEmailConfirmed,
                passwordExpiresAt = @this.passwordExpiresAt,
                twoFactor = @this.twoFactor?.Convert()
            };
        }

        internal static IIdentityAdminIF_v1.ADAuthDTO Convert(this ADAuth @this, LdapDomain ldapDomain)
        {
            return new IIdentityAdminIF_v1.ADAuthDTO()
            {
                id = @this.id,
                etag = @this.etag,
                LastUpdate = @this.LastUpdate,
                isActive = @this.isActive,

                LdapDomainId = @this.LdapDomainId,
                LdapDomainName = ldapDomain?.name,
                userName = @this.userName,
                twoFactor = @this.twoFactor?.Convert(),
            };
        }

        internal static IIdentityAdminIF_v1.KAUAuthDTO Convert(this KAUAuth @this)
        {
            return new IIdentityAdminIF_v1.KAUAuthDTO()
            {
                id = @this.id,
                etag = @this.etag,
                LastUpdate = @this.LastUpdate,
                isActive = @this.isActive,

                email = @this.email,
                KAUUserId = @this.KAUUserId,
                legalName = @this.legalName,
                twoFactor = @this.twoFactor?.Convert(),
            };
        }

        internal static IIdentityAdminIF_v1.CertificateAuthDTO Convert(this CertificateAuth @this)
        {
            return new IIdentityAdminIF_v1.CertificateAuthDTO()
            {
                id = @this.id,
                etag = @this.etag,
                LastUpdate = @this.LastUpdate,
                isActive = @this.isActive,

                certificateThumbprint = @this.certificateThumbprint,
                serialNumber = @this.serialNumber,
                issuer = @this.issuer,
                subject = @this.subject,
                publicKeyHash  = @this.publicKeyHash,
                validFrom = @this.validFrom,
                validUntil = @this.validUntil,
                isRevoked = @this.isRevoked,
                revocationReason = @this.revocationReason,
                revokedAt = @this.revokedAt,
            };
        }

        internal static IIdentityAdminIF_v1.TwoFactorConfigurationDTO Convert(this TwoFactorConfiguration @this)
        {
            return new IIdentityAdminIF_v1.TwoFactorConfigurationDTO()
            {
                enabled = @this.enabled,
                method = @this.method.Convert(),
                email = @this.email,
                phoneNumber = @this.phoneNumber,
            };
        }

        internal static IIdentityAdminIF_v1.AuthDTO.Methods Convert(this Auth.Methods @this)
        {
            return @this switch
            {
                Auth.Methods.Email => IIdentityAdminIF_v1.AuthDTO.Methods.Email,
                Auth.Methods.ActiveDirectory => IIdentityAdminIF_v1.AuthDTO.Methods.ActiveDirectory,
                Auth.Methods.KAU => IIdentityAdminIF_v1.AuthDTO.Methods.KAU,
                Auth.Methods.Certificate => IIdentityAdminIF_v1.AuthDTO.Methods.Certificate,
                _ => throw new NotImplementedException(),
            };
        }

        internal static IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods Convert(this TwoFactorConfiguration.Methods @this)
        {
            return @this switch
            {
                TwoFactorConfiguration.Methods.TOTP => IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP,
                TwoFactorConfiguration.Methods.SMS => IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.SMS,
                TwoFactorConfiguration.Methods.Email => IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.Email,
                _ => throw new NotImplementedException(),
            };
        }
    }
}
