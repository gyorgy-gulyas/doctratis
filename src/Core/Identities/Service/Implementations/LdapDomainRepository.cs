using Core.Identities.Ldap;
using PolyPersist.Net.Extensions;
using ServiceKit.Net;

namespace Core.Identities.Service.Implementations
{
    public class LdapDomainRepository : ILdapDomainRepository
    {
        private readonly IdentityStoreContext _context;

        public LdapDomainRepository(IdentityStoreContext context)
        {
            _context = context;
        }
        
        async Task<Response<LdapDomain>> ILdapDomainRepository.insertLdapDomain(CallingContext ctx, LdapDomain ldapDomain)
        {
            await _context.LdapDomains.Insert(ldapDomain).ConfigureAwait(false);

            _context.Audit_Domain(Auditing.TrailOperations.Create, ctx, ldapDomain);

            return new(ldapDomain);
        }

        async Task<Response<LdapDomain>> ILdapDomainRepository.updateLdapDomain(CallingContext ctx, LdapDomain ldapDomain)
        {
            await _context.LdapDomains.Update(ldapDomain).ConfigureAwait(false);

            _context.Audit_Domain(Auditing.TrailOperations.Update, ctx, ldapDomain);

            return new(ldapDomain);
        }

        async Task<Response<LdapDomain>> ILdapDomainRepository.getLdapDomain(CallingContext ctx, string id)
        {
            var ldapDomain = await _context.LdapDomains.Find(id, id);
            if (ldapDomain == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"LDAP domain '{id}' does not exist" });
            
            return new(ldapDomain);
        }

        Task<Response<List<LdapDomain>>> ILdapDomainRepository.getAllLdapDomain(CallingContext ctx)
        {
            var domains = _context
                .LdapDomains
                .AsQueryable()
                .ToList();

            return Response<List<LdapDomain>>.Success(domains).AsTask();
        }
    }
}
