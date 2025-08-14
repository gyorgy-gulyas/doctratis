using IAM.Identities.Identity;
using IAM.Identities.Ldap;
using PolyPersist.Net.Extensions;
using ServiceKit.Net;

namespace IAM.Identities.Service.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly IdentityStoreContext _context;

        public AccountService(IdentityStoreContext context)
        {
            _context = context;
        }

        async Task<Response<IAccountService.AccountWithAuth>> IAccountService.findAccountByEmailAuth(CallingContext ctx, string email)
        {
            if (string.IsNullOrWhiteSpace(email) == true)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Email cannot be empty" });

            email = email.Trim().ToLowerInvariant();

            var normalizedEmail = email.Trim().ToLowerInvariant();

            var auth = _context.Auths
                .AsQueryable()
                .Where(a => a.method == Auth.Methods.EmailAndPassword)
                .AsEnumerable()
                .OfType<EmailAndPasswordAuth>()
                .Where(a => a.email == email)
                .FirstOrDefault();
            
            if(auth == null )
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Email auth with email '{normalizedEmail}' was not found" });

            var account = await _context.Accounts.Find(auth.accountId, auth.accountId);
            if(account == null )
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Account linked to email '{normalizedEmail}' was not found" });

            return new(new IAccountService.AccountWithAuth() { account = account, auth = auth } );
        }

        async Task<Response<IAccountService.AccountWithAuth>> IAccountService.findAccountByADCredentrials(CallingContext ctx, LdapDomain domain, string userName)
        {
            if (string.IsNullOrWhiteSpace(userName) == true)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"UserName cannot be empty" });

            var normalizedUserName = userName.Trim().ToLowerInvariant();

            var auth = _context.Auths
                .AsQueryable()
                .Where(a => a.method == Auth.Methods.ActiveDirectory)
                .AsEnumerable()
                .OfType<ADAuth>()
                .Where(a => a.LdapDomainId == domain.id && a.userName == normalizedUserName)
                .FirstOrDefault();
            
            if(auth == null )
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"AD auth with email '{domain.name}\\{normalizedUserName}' was not found" });

            var account = await _context.Accounts.Find(auth.accountId, auth.accountId);
            if(account == null )
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Account linked to AD account '{domain.name}\\{normalizedUserName}' was not found" });

            return new(new IAccountService.AccountWithAuth() { account = account, auth = auth } );
        }

        async Task<Response<IAccountService.AccountWithAuth>> IAccountService.findAccountKAUUserId(CallingContext ctx, string kauUserId)
        {
            if (string.IsNullOrWhiteSpace(kauUserId) == true)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"KAU user id cannot be empty" });

            var auth = _context.Auths
                .AsQueryable()
                .Where(a => a.method == Auth.Methods.KAU)
                .AsEnumerable()
                .OfType<KAUAuth>()
                .Where(a => a.KAUUserId == kauUserId)
                .FirstOrDefault();

            if(auth == null )
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"KAÜ auth with id '{kauUserId}' was not found" });

            var account = await _context.Accounts.Find(auth.accountId, auth.accountId);
            if(account == null )
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Account linked to KAÜ account '{kauUserId}' was not found" });

            return new(new IAccountService.AccountWithAuth() { account = account, auth = auth } );
        }
    }
}
