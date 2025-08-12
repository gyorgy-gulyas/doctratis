using Core.Identities.Identity;
using PolyPersist.Net.Extensions;
using ServiceKit.Net;

namespace Core.Identities.Service.Implementations
{
    public class AccountRepository : IAccountRepository
    {
        private readonly IdentityStoreContext _context;

        public AccountRepository(IdentityStoreContext context)
        {
            _context = context;
        }

        async Task<Response<Account>> IAccountRepository.getAccount(CallingContext ctx, string id)
        {
            var account = await _context.Accounts.Find(id, id);
            if (account == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"LDAP domain '{id}' does not exist" });
            
            return new(account);
        }

        Task<Response<List<Account>>> IAccountRepository.getAllAccount(CallingContext ctx)
        {
             var accounts = _context
                .Accounts
                .AsQueryable()
                .ToList();

            return Response<List<Account>>.Success(accounts).AsTask();
        }
    }
}
