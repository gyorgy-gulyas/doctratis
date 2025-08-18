using IAM.Identities.Ldap;
using PolyPersist.Net.Extensions;
using ServiceKit.Net;

namespace IAM.Identities.Service.Implementations
{
    public class LdapDomainService : ILdapDomainService
    {
        private readonly IdentityStoreContext _context;
        private readonly ILdapDomainRepository _ldapDomainRepository;

        public LdapDomainService(IdentityStoreContext context, ILdapDomainRepository ldapDomainRepository)
        {
            _context = context;
            _ldapDomainRepository = ldapDomainRepository;
        }

        async Task<Response<LdapDomain>> ILdapDomainService.RegisterLdapDomain(CallingContext ctx, LdapDomain ldapDomain)
        {
            var name = ldapDomain.name.Normalize().Trim().ToLower();
            var already = _context
                .LdapDomains
                .AsQueryable()
                .Where(ldap => ldap.name.ToLower() == name)
                .FirstOrDefault();

            if(already != null)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"LDAP domain with name '{name}' is already exist " });

            return await _ldapDomainRepository.insertLdapDomain(ctx, ldapDomain).ConfigureAwait(false);
        }

        async Task<Response<LdapDomain>> ILdapDomainService.UpdateRegisteredLdapDomain(CallingContext ctx, LdapDomain ldapDomain)
        {
            var name = ldapDomain.name.Normalize().Trim().ToLower();
            if (string.IsNullOrEmpty(name) == true)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"LDAP domain with name cannot be empty " });

            var already = _context
                .LdapDomains
                .AsQueryable()
                .Where(ldap => ldap.name.ToLower() == name && ldap.id != ldapDomain.id)
                .FirstOrDefault();

            if (already != null)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"LDAP domain with name '{name}' is already exist " });

            return await _ldapDomainRepository.updateLdapDomain(ctx, ldapDomain).ConfigureAwait(false);
        }
    }
}
