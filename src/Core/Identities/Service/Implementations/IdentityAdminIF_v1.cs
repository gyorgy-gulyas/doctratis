using ServiceKit.Net;
using Twilio.Rest.Api.V2010.Account.Usage.Record;

namespace Core.Identities.Service.Implementations
{
    public class IdentityAdminIF_v1 : IIdentityAdminIF_v1
    {

        private readonly IAccountService _accountService;
        public IdentityAdminIF_v1(IAccountService accountService)
        {
            _accountService = accountService;
        }

        async Task<Response<IIdentityAdminIF_v1.LdapDomainDTO>> IIdentityAdminIF_v1.RegisterLdapDomain(CallingContext ctx, IIdentityAdminIF_v1.LdapDomainDTO ldap)
        {
            var domain = ldap.Convert();

            var result = await _accountService.insertLdapDomain(ctx, domain).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            return new(result.Value.Convert());
        }

        async Task<Response<List<IIdentityAdminIF_v1.LdapDomainSummaryDTO>>> IIdentityAdminIF_v1.GetAllRegisteredLdapDomain(CallingContext ctx)
        {
            var result = await _accountService.getAllLdapDomain(ctx).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            return new(result.Value.Select( d => d.ConvertToSummary()).ToList());
        }

        async Task<Response<IIdentityAdminIF_v1.LdapDomainDTO>> IIdentityAdminIF_v1.GetRegisteredLdapDomain(CallingContext ctx, string id)
        {
            var result = await _accountService.getLdapDomain(ctx, id).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            return new(result.Value.Convert());
        }

        async Task<Response<IIdentityAdminIF_v1.LdapDomainDTO>> IIdentityAdminIF_v1.UpdateRegisteredLdapDomain(CallingContext ctx, IIdentityAdminIF_v1.LdapDomainDTO ldap)
        {
            var domain = ldap.Convert();

            var result = await _accountService.updateLdapDomain(ctx, domain).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            return new(result.Value.Convert());
        }

        Task<Response<List<IIdentityAdminIF_v1.AccountSummaryDTO>>> IIdentityAdminIF_v1.getAllAccount(CallingContext ctx)
        {
            throw new NotImplementedException();
        }

        Task<Response<List<IIdentityAdminIF_v1.AccountSummaryDTO>>> IIdentityAdminIF_v1.getAccount(CallingContext ctx, string id)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.AccountDTO>> IIdentityAdminIF_v1.createAccount(CallingContext ctx, IIdentityAdminIF_v1.AccountDTO account)
        {
            throw new NotImplementedException();
        }

        Task<Response<IIdentityAdminIF_v1.AccountDTO>> IIdentityAdminIF_v1.updateAccount(CallingContext ctx, IIdentityAdminIF_v1.AccountDTO account)
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
    }
}
