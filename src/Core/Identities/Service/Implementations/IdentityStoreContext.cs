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
        public readonly IDocumentCollection<LdapDomain> LdapDomains;
        public readonly IColumnTable<LoginAuditEventLog> LoginAuditEventLogs;

        public IdentityStoreContext(IStoreProvider storeProvider, IAuditEntryContainer auditEntryContainer) 
            : base(storeProvider)
        {
            _auditEntryContainer = auditEntryContainer;

            Accounts = base.GetOrCreateDocumentCollection<Account>().Result;
            LdapDomains = base.GetOrCreateDocumentCollection<LdapDomain>().Result;

            LoginAuditEventLogs = base.GetOrCreateColumnTable<LoginAuditEventLog>().Result;
        }

        internal void AuditLog_LoggedIn(CallingContext ctx, Account loginedAccount, Auth usedAuth  )
        {
            _auditEntryContainer.AddEntryForBackgrondSave(new LoginEventLog(this, ctx, "logged_in" )
            {
                _account = loginedAccount,
                _authMethod = usedAuth.method,
            });
        }

        internal void AuditLog_SignInFailed(CallingContext ctx, Account account, Auth usedAuth, ILoginIF_v1.SignInResult result)
        {
            _auditEntryContainer.AddEntryForBackgrondSave(new LoginEventLog(this, ctx, "singin_failed")
            {
                _account = account,
                _authMethod = usedAuth.method,
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

            protected override string GetLogSpecificPayloadJSON() => JsonSerializer.Serialize( _jsonData );
            protected override string GetPartitionKey() => _account.id;
            protected override IColumnTable<LoginAuditEventLog> GetTable() => _storeContext.LoginAuditEventLogs;
        }
    }
}
