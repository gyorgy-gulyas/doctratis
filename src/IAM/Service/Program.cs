using Core.Auditing.Worker;
using Core.Base.Agents.Communication;
using IAM.Identities;
using IAM.Identities.Context.Implementations;
using IAM.Identities.Service;
using IAM.Identities.Service.Implementations;
using IAM.Identities.Service.Implementations.Helpers;
using PolyPersist;
using ServiceKit.Net;
using ServiceKit.Net.Communicators;


BaseServiceHost.Create<IAMServiceHost>(args, new BaseServiceHost.Options()
{
    WithAuthentication = false,
    WithGrpc = true,
    WithRest = true,
    WithReponseCompression = false,
    PathBase = "/iam"
}).Run();

public class IAMServiceHost : BaseServiceHost
{
    protected override void _BeforeAddServices(IServiceCollection services, Options options)
    {
    }

    protected override void _AfterAddServices(IServiceCollection services, Options options)
    {
        services.AddSingleton<IStoreProvider>(new IdentityStoreProvider());
        services.AddSingleton<IdentityStoreContext>();
        services.AddAuditWorker();

        services.UseSms_Twilio();
        services.AddSingleton<SmsAgent>();
        services.UseEmail_Graph();
        services.AddSingleton<EmailAgent>();
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
        //services.AddHttpClient<ICertificateAuthorityACL, CertificateAuthorityACL_AD>();
        //services.AddHttpClient<ICertificateAuthorityACL, CertificateAuthorityACL_HasiCorp>();
        services.AddSingleton<ICertificateAuthorityACL, CertificateAuthorityACL_BouncyCastle>();
    }

    protected override void _BeforeBuild(IServiceCollection services, Options options)
    {
    }

    protected override void _AfterBuild(IServiceCollection services, Options options)
    {
    }
}

namespace IAM.Identities.Service
{
    using PolyPersist.Net.BlobStore.Memory;
    using PolyPersist.Net.ColumnStore.Memory;
    using PolyPersist.Net.Core;
    using PolyPersist.Net.DocumentStore.Memory;

    public class IdentityStoreProvider : StoreProvider
    {
        //        protected override IDocumentStore GetDocumentStore() => new MongoDB_DocumentStore("mongodb://127.0.0.1:27617/?directConnection=true");
        //        protected override IBlobStore GetBlobStore() => new GridFS_BlobStore("mongodb://127.0.0.1:27617/?directConnection=true");

        protected override IDocumentStore GetDocumentStore() => new Memory_DocumentStore("");
        protected override IBlobStore GetBlobStore() => new Memory_BlobStore("");
        protected override IColumnStore GetColumnStore() => new Memory_ColumnStore("");
    }
}
