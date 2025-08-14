using ServiceKit.Net;

namespace IAM.Identities.Service.Implementations
{
    public class IdentityAdminIF_v1 : IIdentityAdminIF_v1
    {
        private readonly IAccountService _accountService;
        private readonly IAccountRepository _accountRepository;
        private readonly ILdapDomainRepository _ldapDomainRepository;

        public IdentityAdminIF_v1(IAccountService accountService, IAccountRepository accountRepository, ILdapDomainRepository ldapDomainRepository)
        {
            _accountService = accountService;
            _accountRepository = accountRepository;
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

        internal static Identity.Account Convert(this IIdentityAdminIF_v1.AccountDTO @this, Identity.Account original)
        {
            return new()
            {
                id = @this.id,
                etag = @this.etag,
                LastUpdate = @this.LastUpdate,
                Name = @this.Name,
                isActive = @this.isActive,
                contacts = @this.contacts.Select(c => c.Convert()).ToList(),
                Type = @this.Type.Convert(),
                accountSecret = original?.accountSecret,
            };
        }

        internal static IIdentityAdminIF_v1.AccountDTO Convert(this Identity.Account @this)
        {
            return new()
            {
                id = @this.id,
                etag = @this.etag,
                LastUpdate = @this.LastUpdate,
                Name = @this.Name,
                isActive = @this.isActive,
                contacts = @this.contacts.Select(c => c.Convert()).ToList(),
                Type = @this.Type.Convert(),
            };
        }

        internal static IIdentityAdminIF_v1.AccountSummaryDTO ConvertToSummary(this Identity.Account @this)
        {
            return new()
            {
                id = @this.id,
                Name = @this.Name,
                isActive = @this.isActive,
                Type = @this.Type.Convert(),
            };
        }

        internal static Identity.ContactInfo Convert(this IIdentityAdminIF_v1.ContactInfo @this)
        {
            return new()
            {
                contactType = @this.contactType,
                email = @this.email,
                phoneNumber = @this.phoneNumber,
            };
        }

        internal static IIdentityAdminIF_v1.ContactInfo Convert(this Identity.ContactInfo @this)
        {
            return new()
            {
                contactType = @this.contactType,
                email = @this.email,
                phoneNumber = @this.phoneNumber,
            };
        }

        internal static Identity.Account.Types Convert(this IIdentityAdminIF_v1.AccountTypesDTO @this)
        {
            return @this switch
            {
                IIdentityAdminIF_v1.AccountTypesDTO.User => Identity.Account.Types.User,
                IIdentityAdminIF_v1.AccountTypesDTO.ExternalSystem => Identity.Account.Types.ExternalSystem,
                IIdentityAdminIF_v1.AccountTypesDTO.InternalService => Identity.Account.Types.InternalService,
                _ => throw new NotImplementedException(),
            };
        }

        internal static IIdentityAdminIF_v1.AccountTypesDTO Convert(this Identity.Account.Types @this)
        {
            return @this switch
            {
                Identity.Account.Types.User => IIdentityAdminIF_v1.AccountTypesDTO.User,
                Identity.Account.Types.ExternalSystem => IIdentityAdminIF_v1.AccountTypesDTO.ExternalSystem,
                Identity.Account.Types.InternalService => IIdentityAdminIF_v1.AccountTypesDTO.InternalService,
                _ => throw new NotImplementedException(),
            };
        }
    }
}
