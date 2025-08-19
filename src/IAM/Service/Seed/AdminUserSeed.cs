using IAM.Identities.Identity;
using IAM.Identities.Service.Implementations;
using IAM.Identities.Service.Implementations.Helpers;
using PolyPersist.Net.Extensions;
using PolyPersist.Net.Transactions;
using System.Security.Cryptography;

namespace IAM.Service.Seed
{
    internal class AdminUserSeed
    {
        private readonly ILogger<AdminUserSeed> _logger;
        private readonly IdentityStoreContext _context;
        private readonly PasswordAgent _passwordAgent;

        internal AdminUserSeed(
            ILogger<AdminUserSeed> logger,
            IdentityStoreContext context,
            PasswordAgent passwordAgent)
        {
            _logger = logger;
            _context = context;
            _passwordAgent = passwordAgent;
        }

        internal async Task Execute()
        {
            var system_user = _context
                .Accounts
                .AsQueryable()
                .Where(acc =>
                    acc.Type == Account.Types.User &&
                    acc.Name == "docratis" )
                .FirstOrDefault();

            if (system_user != null)
            {
                _logger.LogInformation("Docratis system user has already been created, no seeding action is required");
                return;
            }

            await using var tx = new Transaction();
            var account = new Account()
            {
                id = Guid.NewGuid().ToString(),
                etag = null,
                LastUpdate = DateTime.UtcNow,

                isActive = true,
                Name = "docratis",
                Type = Account.Types.User,
                accountSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
                contacts = [],
            };
            await tx.Insert(_context.Accounts, account);

            var salt = _passwordAgent.CreateLifetimeSalt();
            var passwordHash = _passwordAgent.GeneratePasswordHash("Docratis#2025", salt);
            var auth = new EmailAuth()
            {
                id = Guid.NewGuid().ToString(),
                etag = null,
                LastUpdate = DateTime.UtcNow,

                accountId = account.id,
                isActive = true,
                method = Auth.Methods.Email,
                email = "docratis@docratis.com",
                isEmailConfirmed = true,
                passwordExpiresAt = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                passwordHash = passwordHash.Value,
                passwordSalt = salt,
                passwordHistory = [],
                twoFactor = new TwoFactorConfiguration()
                {
                    enabled = false,
                }
            };
            await tx.Insert(_context.Auths, auth);
            await tx.Commit();

            _logger.LogInformation(message: $"Docratis system user has been created with id: {account.id}");
        }
    }
}
