using IAM.Identities.Identity;
using IAM.Identities.Ldap;
using PolyPersist.Net.Extensions;
using ServiceKit.Net;
using System.Diagnostics.Tracing;
using System.Security.Cryptography;

namespace IAM.Identities.Service.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly IdentityStoreContext _context;
        private readonly IAccountRepository _accountRepository;

        public AccountService(IdentityStoreContext context, IAccountRepository accountRepository)
        {
            _context = context;
            _accountRepository = accountRepository;
        }

        async Task<Response<IAccountService.AccountWithAuth>> IAccountService.findAccountByEmailAuth(CallingContext ctx, string email)
        {
            if (string.IsNullOrWhiteSpace(email) == true)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Email cannot be empty" });

            email = email.Trim().ToLowerInvariant();

            var auth = _context.Auths
                .AsQueryable<EmailAuth,Auth>()
                .Where(a => 
                    a.method == Auth.Methods.Email &&
                    a.email.ToLower() == email)
                .FirstOrDefault();

            if (auth == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Email auth with email '{email}' was not found" });

            var account = await _context.Accounts.Find(auth.accountId, auth.accountId);
            if (account == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Account linked to email '{email}' was not found" });

            return new(new IAccountService.AccountWithAuth() { account = account, auth = auth });
        }

        async Task<Response<IAccountService.AccountWithAuth>> IAccountService.findAccountByADCredentrials(CallingContext ctx, LdapDomain domain, string userName)
        {
            if (string.IsNullOrWhiteSpace(userName) == true)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"UserName cannot be empty" });

            var normalizedUserName = userName.Trim().ToLowerInvariant();

            var auth = _context.Auths
                .AsQueryable<ADAuth,Auth>()
                .Where(a => 
                    a.method == Auth.Methods.ActiveDirectory &&
                    a.LdapDomainId == domain.id && 
                    a.userName == normalizedUserName )
                .FirstOrDefault();

            if (auth == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"AD auth with email '{domain.name}\\{normalizedUserName}' was not found" });

            var account = await _context.Accounts.Find(auth.accountId, auth.accountId);
            if (account == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Account linked to AD account '{domain.name}\\{normalizedUserName}' was not found" });

            return new(new IAccountService.AccountWithAuth() { account = account, auth = auth });
        }

        async Task<Response<IAccountService.AccountWithAuth>> IAccountService.findAccountKAUUserId(CallingContext ctx, string kauUserId)
        {
            if (string.IsNullOrWhiteSpace(kauUserId) == true)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"KAU user id cannot be empty" });

            var auth = _context.Auths
                .AsQueryable<KAUAuth,Auth>()
                .Where(a => 
                    a.method == Auth.Methods.KAU &&
                    a.KAUUserId == kauUserId )
                .FirstOrDefault();

            if (auth == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"KAÜ auth with id '{kauUserId}' was not found" });

            var account = await _context.Accounts.Find(auth.accountId, auth.accountId);
            if (account == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Account linked to KAÜ account '{kauUserId}' was not found" });

            return new(new IAccountService.AccountWithAuth() { account = account, auth = auth });
        }

        async Task<Response<Account>> IAccountService.createAccount(CallingContext ctx, IAccountService.AccountData data)
        {
            var already = await _accountRepository.findByName(ctx, data.Name);
            if (already.IsFailed())
                return new(already.Error);
            if (already.Value != null)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Account with name '{data.Name}' is already exist" });

            var account = new Account()
            {
                id = Guid.NewGuid().ToString(),
                Name = data.Name,
                Type = data.Type,
                contacts = data.contacts,
                isActive = true,
                accountSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
            };
            var create = await _accountRepository.createAccount(ctx, account);
            if(create.IsFailed())
                return new(create.Error);

            return new(account);
        }

        async Task<Response<Account>> IAccountService.updateAccount(CallingContext ctx, string accountId, string etag, IAccountService.AccountData data)
        {
            var already = await _accountRepository.findByName(ctx, data.Name);
            if (already.IsFailed())
                return new(already.Error);
            if (already.Value != null && already.Value.id != accountId)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Account with name '{data.Name}' is already exist" });

            var original = await _accountRepository.getAccount(ctx, accountId);
            if (original.IsFailed())
                return new(original.Error);

            var account = new Account()
            {
                id = accountId,
                etag = etag,
                Name = data.Name,
                Type = data.Type,
                contacts = data.contacts,
                isActive = original.Value.isActive,
                accountSecret = original.Value.accountSecret,
            };
            var update = await _accountRepository.updateAccount(ctx, account);
            if(update.IsFailed())
                return new(update.Error);

            return new(account);
        }
    }
}
