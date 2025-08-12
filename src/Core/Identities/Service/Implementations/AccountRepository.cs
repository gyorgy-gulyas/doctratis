using Core.Identities.Identity;
using Core.Identities.Ldap;
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

        Task<Response<List<Account>>> IAccountRepository.getAllAccount(CallingContext ctx)
        {
            throw new NotImplementedException();
        }

        Task<Response<List<Account>>> IAccountRepository.getAccount(CallingContext ctx, string id)
        {
            throw new NotImplementedException();
        }

        Task<Response<Account>> IAccountRepository.createAccount(CallingContext ctx, Account account)
        {
            throw new NotImplementedException();
        }

        Task<Response<Account>> IAccountRepository.updateAccount(CallingContext ctx, Account account)
        {
            throw new NotImplementedException();
        }
    }
}
