using IAM.Identities.Ldap;
using PolyPersist.Net.Extensions;
using ServiceKit.Net;

namespace IAM.Identities.Service.Implementations
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


            try
            {

                ldapDomain.id = Guid.NewGuid().ToString();
                await _context.LdapDomains.Insert(ldapDomain).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new(new Error() { Status = Statuses.BadRequest, MessageText = ex.Message, AdditionalInformation = ex.ToString() });
            }

            _context.Audit_Domain(Core.Auditing.TrailOperations.Create, ctx, ldapDomain);

            return new(ldapDomain);
        }

        async Task<Response<LdapDomain>> ILdapDomainRepository.updateLdapDomain(CallingContext ctx, LdapDomain ldapDomain)
        {
            var oroginal = await _context.LdapDomains.Find(ldapDomain.PartitionKey, ldapDomain.id);
            if (oroginal == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"LDAP domain '{ldapDomain.id}' does not exist" });

            try
            {

                await _context.LdapDomains.Update(ldapDomain).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new(new Error() { Status = Statuses.BadRequest, MessageText = ex.Message, AdditionalInformation = ex.ToString() });
            }

            _context.Audit_Domain(Core.Auditing.TrailOperations.Update, ctx, ldapDomain);

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
