using IAM.Identities.Identity;
using IAM.Identities.Service.Implementations;
using IAM.Identities.Service.Implementations.Helpers;
using PolyPersist.Net.Extensions;
using PolyPersist.Net.Transactions;
using System.Security.Cryptography;

namespace IAM.Service.Seed
{
    public class AdminUserSeed
    {
        // The one-off password of the seeded 'docratis' account. It is only read on a FIRST start -
        // once the account exists, seeding returns early and never looks at it.
        private const string InitialPasswordKey = "Seed:AdminPassword";

        private readonly ILogger<AdminUserSeed> _logger;
        private readonly IdentityStoreContext _context;
        private readonly PasswordAgent _passwordAgent;
        private readonly IConfiguration _configuration;

        public AdminUserSeed(
            ILogger<AdminUserSeed> logger,
            IdentityStoreContext context,
            PasswordAgent passwordAgent,
            IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _passwordAgent = passwordAgent;
            _configuration = configuration;
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

            // Refuse to invent one: a default here would be a known credential on every install.
            // This only stops a FIRST start, so an already seeded deployment is never affected.
            var initialPassword = _configuration[InitialPasswordKey];
            if (string.IsNullOrWhiteSpace(initialPassword) == true)
                throw new InvalidOperationException($"The initial password of the 'docratis' account is not configured. Set '{InitialPasswordKey}' in configuration, or the '{InitialPasswordKey.Replace(":", "__")}' environment variable, before the first start.");

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
            var passwordHash = _passwordAgent.GeneratePasswordHash(initialPassword, salt);
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
