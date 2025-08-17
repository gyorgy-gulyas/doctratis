using IAM.Identities.Identity;
using PolyPersist.Net.Extensions;
using ServiceKit.Net;

namespace IAM.Identities.Service.Implementations
{
    public class AccountRepository : IAccountRepository
    {
        private readonly IdentityStoreContext _context;

        public AccountRepository(IdentityStoreContext context)
        {
            _context = context;
        }

        async Task<Response<Account>> IAccountRepository.createAccount(CallingContext ctx, Account account)
        {
            await _context.Accounts.Insert(account);
            _context.Audit_Account(Core.Auditing.TrailOperations.Create, ctx, account);

            return new(account);
        }

        async Task<Response<Account>> IAccountRepository.updateAccount(CallingContext ctx, Account account)
        {
            var original = await _context.Accounts.Find(account.PartitionKey, account.id);
            if (original == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Account '{account.id}' does not exist" });

            await _context.Accounts.Update(account);
            _context.Audit_Account(Core.Auditing.TrailOperations.Update, ctx, account);

            return new(account);
        }

        async Task<Response<Account>> IAccountRepository.getAccount(CallingContext ctx, string id)
        {
            var account = await _context.Accounts.Find(id, id);
            if (account == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Account '{id}' does not exist" });

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

        Task<Response<Account>> IAccountRepository.findByName(CallingContext ctx, string name)
        {
            name = name.Normalize().Trim();

            var already = _context
                .Accounts
                .AsQueryable()
                .Where(a => a.Name == name)
                .FirstOrDefault();

            return Response<Account>.Success(already).AsTask();
        }
    }
}
