using Core.Identities.Identity;
using Core.Identities.Ldap;
using Core.Identities.Service.Implementations.Helpers;
using PolyPersist.Net.Extensions;
using ServiceKit.Net;

namespace Core.Identities.Service.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly IdentityStoreContext _context;

        public AccountService(IdentityStoreContext context)
        {
            _context = context;
        }

        Task<Response<Account>> IAccountService.findUserByEmail(CallingContext ctx, string email)
        {
            if (string.IsNullOrEmpty(email) == true)
                return Response<Account>.Failure(new Error() { Status = Statuses.BadRequest, MessageText = $"Email cannot be empty" }).AsTask();

            email = email.Trim().ToLowerInvariant();

            var account = _context.Accounts
                .AsQueryable()
                .Where(ac =>
                    ac.Type == AccountTypes.User &&
                    ac.emailAndPasswordAuth != null &&
                    ac.emailAndPasswordAuth.email == email)
                .FirstOrDefault();

            return Response<Account>.Success(account).AsTask();
        }

        Task<Response<Account>> IAccountService.findUserByADCredentrials(CallingContext ctx, LdapDomain domain, string userName)
        {
            if (string.IsNullOrEmpty(userName) == true)
                return Response<Account>.Failure(new Error() { Status = Statuses.BadRequest, MessageText = $"UserName cannot be empty" }).AsTask();

            var account = _context.Accounts
                .AsQueryable()
                .Where(ac =>
                    ac.Type == AccountTypes.User &&
                    ac.adAuth != null &&
                    ac.adAuth.LdapDomainId == domain.id &&
                    ac.adAuth.userName == userName)
                .FirstOrDefault();

            return Response<Account>.Success(account).AsTask();
        }

        Task<Response<Account>> IAccountService.findUserKAUUserId(CallingContext ctx, string kauUserId)
        {
            if (string.IsNullOrEmpty(kauUserId) == true)
                return Response<Account>.Failure(new Error() { Status = Statuses.BadRequest, MessageText = $"KAU user id cannot be empty" }).AsTask();

            var account = _context.Accounts
                .AsQueryable()
                .Where(ac =>
                    ac.Type == AccountTypes.User &&
                    ac.kauAuth != null &&
                    ac.kauAuth.KAUUserId == kauUserId )
                .FirstOrDefault();

            return Response<Account>.Success(account).AsTask();
        }
    }
}
