using IAM.Identities.Ldap;
using Novell.Directory.Ldap;
using PolyPersist.Net.Extensions;

namespace IAM.Identities.Service.Implementations.Helpers
{
    public class LdapAuthenticator
    {
        private readonly IdentityStoreContext _context;

        public LdapAuthenticator(IdentityStoreContext context)
        {
            _context = context;
        }

        public (string domain, string user) SplitDomainAndUser(string usernameInput)
        {
            if (string.IsNullOrWhiteSpace(usernameInput))
                throw new ArgumentException("Username cannot be empty.");

            // DOMAIN\User formátum feldarabolása
            var parts = usernameInput.Split('\\', 2);

            if (parts.Length == 2)
            {
                // Első rész: DOMAIN, második: user
                return (parts[0], parts[1]);
            }

            // Ha nincs domain rész, csak user
            return (null, usernameInput);
        }

        public LdapDomain findDomain(string domain)
        {
            domain = domain.Trim().Normalize().ToLower();

            return _context
                .LdapDomains
                .AsQueryable()
                .Where( d =>
                        string.Equals(d.name, domain, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(d.netbiosName, domain, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }

        public async Task<bool> AuthenticateUser(LdapDomain domain, string userName, string password)
        {
            // 2. Try each Domain Controller
            foreach (var dc in domain.domainControllers)
            {
                string host = dc.host;
                int port = dc.port;

                using (var connection = new LdapConnection())
                {
                    try
                    {
                        // 3. Connect
                        await connection.ConnectAsync(host, port);

                        // 4. Build userPrincipalName (user@domain.local)
                        string userDn = userName.Contains("@")
                            ? userName
                            : $"{userName}@{domain.name}";

                        // 5. Bind using user credentials
                        await connection.BindAsync(userDn, password);

                        if (connection.Bound)
                            return true;
                    }
                    catch (LdapException)
                    {
                        continue;
                    }
                }
            }

            return false;
        }
    }
}

