using Core.Auditing;
using Core.Auditing.Worker;
using Core.Identities.Identity;
using Core.Identities.Ldap;
using PolyPersist;
using PolyPersist.Net.Context;
using ServiceKit.Net;
using System.Text.Json;

namespace Core.Identities.Service.Implementations
{
    public class IdentityStoreContext : StoreContext
    {
        private readonly IAuditEntryContainer _auditEntryContainer;
        public readonly IDocumentCollection<Account> Accounts;
        public readonly IDocumentCollection<Auth> Auths;
        public readonly IDocumentCollection<LdapDomain> LdapDomains;
        public readonly IColumnTable<LoginAuditEventLog> LoginAuditEventLogs;
        public readonly IColumnTable<LdapDomainAuditTrail> LdapDomainAuditTrails;
        public readonly IColumnTable<AccountAuditTrail> AccountAuditTrails;

        public IdentityStoreContext(IStoreProvider storeProvider, IAuditEntryContainer auditEntryContainer)
            : base(storeProvider)
        {
            _auditEntryContainer = auditEntryContainer;

            Accounts = base.GetOrCreateDocumentCollection<Account>().Result;
            Auths = base.GetOrCreateDocumentCollection<Auth>().Result;
            LdapDomains = base.GetOrCreateDocumentCollection<LdapDomain>().Result;

            LoginAuditEventLogs = base.GetOrCreateColumnTable<LoginAuditEventLog>().Result;
            LdapDomainAuditTrails = base.GetOrCreateColumnTable<LdapDomainAuditTrail>().Result;
            AccountAuditTrails = base.GetOrCreateColumnTable<AccountAuditTrail>().Result;
        }

        #region Login Audit
        internal void AuditLog_LoggedIn(CallingContext ctx, Account loginedAccount, Auth.Methods usedAuthMetod)
        {
            _auditEntryContainer.AddEntryForBackgrondSave(new LoginEventLog(this, ctx, "logged_in")
            {
                _account = loginedAccount,
                _authMethod = usedAuthMetod,
            });
        }

        internal void AuditLog_SignInFailed(CallingContext ctx, Account account, Auth.Methods usedAuthMethod, ILoginIF_v1.SignInResult result)
        {
            _auditEntryContainer.AddEntryForBackgrondSave(new LoginEventLog(this, ctx, "singin_failed")
            {
                _account = account,
                _authMethod = usedAuthMethod,
                _jsonData = new { result }
            });
        }

        internal void AuditLog_2FASuccess(CallingContext ctx, Account account)
        {
            _auditEntryContainer.AddEntryForBackgrondSave(new LoginEventLog(this, ctx, "2fa_sucess")
            {
                _account = account,
                _authMethod = default,
                _jsonData = string.Empty
            });
        }

        internal void AuditLog_2FAFailed(CallingContext ctx, Account account, TwoFactorConfiguration.Method method)
        {
            _auditEntryContainer.AddEntryForBackgrondSave(new LoginEventLog(this, ctx, "2fa_failed")
            {
                _account = account,
                _jsonData = new { method }
            });
        }

        internal void AuditLog_TokenRefreshed(CallingContext ctx, Account account)
        {
            _auditEntryContainer.AddEntryForBackgrondSave(new LoginEventLog(this, ctx, "token_refreshed")
            {
                _account = account,
                _jsonData = string.Empty
            });
        }

        internal void AuditLog_2FASent(CallingContext ctx, Account account, string to)
        {
            _auditEntryContainer.AddEntryForBackgrondSave(new LoginEventLog(this, ctx, "2fa_sent")
            {
                _account = account,
                _jsonData = new { to }
            });
        }

        public class LoginEventLog : AuditLog<LoginAuditEventLog>
        {
            private readonly IdentityStoreContext _storeContext;
            internal Account _account;
            internal Auth.Methods _authMethod;
            internal object _jsonData;

            public LoginEventLog(IdentityStoreContext storeContext, CallingContext callingContext, string operation)
                : base(callingContext, operation)
            {
                _storeContext = storeContext;
            }

            protected override void FillAddtionalMembers(LoginAuditEventLog log)
            {
                log.AccountType = _account.Type;
                log.authMethod = _authMethod;
            }

            protected override string GetLogSpecificPayloadJSON() => JsonSerializer.Serialize(_jsonData);
            protected override string GetPartitionKey() => _account.id;
            protected override IColumnTable<LoginAuditEventLog> GetTable() => _storeContext.LoginAuditEventLogs;
        }
        #endregion Login Audit


        #region Domain Audit
        internal void Audit_Domain(TrailOperations operation, CallingContext ctx, LdapDomain domain)
        {
            _auditEntryContainer.AddEntryForBackgrondSave(new LdapDomainAuditEntry(this, ctx, operation)
            {
                _domain = domain,
            });
        }

        public class LdapDomainAuditEntry : AuditTrail<LdapDomainAuditTrail>
        {
            private readonly IdentityStoreContext _storeContext;
            public LdapDomain _domain { get; set; }

            public LdapDomainAuditEntry(IdentityStoreContext storeContext, CallingContext callingContext, TrailOperations operation)
                : base(callingContext, operation)
            {
                _storeContext = storeContext;
            }

            protected override Task InitializeAsync() => Task.CompletedTask;

            protected override void FillAddtionalMembers(LdapDomainAuditTrail trail)
            {
                trail.domainId = _domain.id;
                trail.domainName = _domain.name;
            }

            protected override IEntity GetRootEntity() => _domain;
            protected override string GetEntitySpecificPayloadJSON() => JsonSerializer.Serialize(new { _domain });
            protected override IColumnTable<LdapDomainAuditTrail> GetTable() => _storeContext.LdapDomainAuditTrails;
        }
        #endregion Domain Audit

        #region Account Audit
        internal void Audit_Account(TrailOperations operation, CallingContext ctx, Account account)
        {
            _auditEntryContainer.AddEntryForBackgrondSave(new AccountAuditEntry(this, ctx, operation)
            {
                _account = account,
            });
        }

        public class AccountAuditEntry : AuditTrail<AccountAuditTrail>
        {
            private readonly IdentityStoreContext _storeContext;
            public Account _account { get; set; }

            public AccountAuditEntry(IdentityStoreContext storeContext, CallingContext callingContext, TrailOperations operation)
                : base(callingContext, operation)
            {
                _storeContext = storeContext;
            }

            protected override Task InitializeAsync() => Task.CompletedTask;

            protected override void FillAddtionalMembers(AccountAuditTrail trail)
            {
                trail.accountId = _account.id;
                trail.accountName = _account.Name;
            }

            protected override IEntity GetRootEntity() => _account;
            protected override string GetEntitySpecificPayloadJSON() => JsonSerializer.Serialize(new { _account });
            protected override IColumnTable<AccountAuditTrail> GetTable() => _storeContext.AccountAuditTrails;
        }
        #endregion Account Audit
    }
}
