using Core.Auditing.Worker;
using Core.Base.Agents.Communication;
using IAM.Identities.Context.Implementations;
using IAM.Identities.Service.Implementations;
using IAM.Identities.Service.Implementations.Helpers;
using Microsoft.Extensions.DependencyInjection;
using PolyPersist;
using PolyPersist.Net.BlobStore.Memory;
using PolyPersist.Net.ColumnStore.Memory;
using PolyPersist.Net.Core;
using PolyPersist.Net.DocumentStore.Memory;
using ServiceKit.Net.Communicators;
using System;

namespace IAM.Identities.Tests
{
    [TestClass]
    public class TestMain
    {
        public static ServiceProvider ServiceProvider = null;

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
            // acls
            services.AddHttpClient<ICertificateAuthorityACL, CertificateAuthorityACL_AD>();
            //services.AddHttpClient<ICertificateAuthorityACL, CertificateAuthorityACL_HasiCorp>();

            ServiceProvider = services.BuildServiceProvider();

            return Task.CompletedTask;
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
