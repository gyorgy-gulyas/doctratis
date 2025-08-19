using Core.Auditing.Worker;
using Core.Base.Agents.Communication;
using IAM.Identities.Service.Implementations;
using IAM.Identities.Service.Implementations.Helpers;
using IAM.Identities.Tests.Mock;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PolyPersist;
using PolyPersist.Net.BlobStore.Memory;
using PolyPersist.Net.ColumnStore.Memory;
using PolyPersist.Net.Core;
using PolyPersist.Net.DocumentStore.Memory;
using PolyPersist.Net.Extensions;
using ServiceKit.Net;
using ServiceKit.Net.Communicators;

namespace IAM.Identities.Tests
{
    [TestClass]
    public class TestMain
    {
        public static ServiceProvider ServiceProvider = null;
        internal static CallingContext ctx;

        static TestMain()
        {
        }

        [AssemblyInitialize]
        public static Task InitializeContext(TestContext tc)
        {
            var services = new ServiceCollection();

            services.AddSingleton<IStoreProvider>(new Core.Test.MemoryStoreProvider());
            services.AddSingleton<IAuditEntryContainer>(new Core.Test.Mock.Mock_AuditEntryContainer());
            services.AddSingleton<ISmsCommunicator, Core.Test.Mock.Mock_SmsCommunicator>();
            services.AddSingleton<IEmailCommunicator, Core.Test.Mock.Mock_EmailCommunicator>();
            services.AddSingleton<IConfiguration>(new Core.Test.Mock.Mock_Configuration(new Dictionary<string, string>
            {
                ["App:Name"] = "Doctratis",
            }));
            services.AddSingleton<SmsAgent>();
            services.AddSingleton<EmailAgent>();

            services.AddSingleton<IdentityStoreContext>();
            services.AddSingleton<TokenAgent>();
            services.AddSingleton<PasswordAgent>();
            services.AddSingleton<CertificateAgent>();
            services.AddSingleton<LdapAuthenticator>();
            services.AddHttpClient<KAUAuthenticator>();
            services.AddSingleton<KAUAuthenticator>();

            // interfaces
            services.AddSingleton<ILoginIF_v1, LoginIF_v1>();
            services.AddSingleton<IIdentityAdminIF_v1, IdentityAdminIF_v1>();
            // repositories
            services.AddSingleton<IAccountRepository, AccountRepository>();
            services.AddSingleton<IAuthRepository, AuthRepository>();
            services.AddSingleton<ILdapDomainRepository, LdapDomainRepository>();
            // services
            services.AddSingleton<IAccountService, AccountService>();
            services.AddSingleton<IAccountAuthService, AccountAuthService>();
            services.AddSingleton<ILoginService, LoginService>();
            services.AddSingleton<ILdapDomainService, LdapDomainService>();
            // acls
            //services.AddHttpClient<ICertificateAuthorityACL, CertificateAuthorityACL_AD>();
            //services.AddHttpClient<ICertificateAuthorityACL, CertificateAuthorityACL_HasiCorp>();
            //services.AddSingleton<ICertificateAuthorityACL, CertificateAuthorityACL_BouncyCastle>();
            services.AddSingleton<ICertificateAuthorityACL, Mock_CertificateAuthorityACL>();

            ServiceProvider = services.BuildServiceProvider();

            ctx = new CallingContext().CloneWithIdentity("test-user-id", "test-user-name", CallingContext.IdentityTypes.User);
            return Task.CompletedTask;
        }

        internal static async Task DeleteAllData()
        {
            var context = ServiceProvider.GetRequiredService<IdentityStoreContext>();

            foreach (var entity in context.Accounts.AsQueryable().ToArray())
                await context.Accounts.Delete(entity.PartitionKey, entity.id);

            foreach (var entity in context.Auths.AsQueryable().ToArray())
                await context.Auths.Delete(entity.PartitionKey, entity.id);

            foreach (var entity in context.LdapDomains.AsQueryable().ToArray())
                await context.LdapDomains.Delete(entity.PartitionKey, entity.id);

            foreach (var entity in context.LoginAuditEventLogs.AsQueryable().ToArray())
                await context.LoginAuditEventLogs.Delete(entity.PartitionKey, entity.id);

            foreach (var entity in context.LdapDomainAuditTrails.AsQueryable().ToArray())
                await context.LdapDomainAuditTrails.Delete(entity.PartitionKey, entity.id);

            foreach (var entity in context.AccountAuditTrails.AsQueryable().ToArray())
                await context.AccountAuditTrails.Delete(entity.PartitionKey, entity.id);
        }
    }


    public class IdentityStoreProvider : StoreProvider
    {
        //        protected override IDocumentStore GetDocumentStore() => new MongoDB_DocumentStore("mongodb://127.0.0.1:27617/?directConnection=true");
        //        protected override IBlobStore GetBlobStore() => new GridFS_BlobStore("mongodb://127.0.0.1:27617/?directConnection=true");

        protected override IDocumentStore GetDocumentStore() => new Memory_DocumentStore("");
        protected override IBlobStore GetBlobStore() => new Memory_BlobStore("");
        protected override IColumnStore GetColumnStore() => new Memory_ColumnStore("");
    }
}
