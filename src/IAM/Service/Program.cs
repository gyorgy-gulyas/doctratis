using Core.Auditing.Worker;
using Core.Base.Agents.Communication;
using IAM.Identities;
using IAM.Identities.Context.Implementations;
using IAM.Identities.Service;
using IAM.Identities.Service.Implementations;
using IAM.Identities.Service.Implementations.Helpers;
using IAM.Service.Seed;
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
        services.UseSms_Twilio();
        services.UseEmail_Graph();
    }

    protected override void _AfterAddServices(IServiceCollection services, Options options)
    {
        services.AddSingleton<IStoreProvider>(provider => new IdentityStoreProvider(provider.GetRequiredService<IConfiguration>()));
        services.AddSingleton<IdentityStoreContext>();
        services.AddAuditWorker();

        // helpers
        services.AddSingleton<SmsAgent>();
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
        services.AddSingleton<ILdapDomainService, LdapDomainService>();
        
        // acls
        //services.AddHttpClient<ICertificateAuthorityACL, CertificateAuthorityACL_AD>();
        //services.AddHttpClient<ICertificateAuthorityACL, CertificateAuthorityACL_HasiCorp>();
        services.AddSingleton<ICertificateAuthorityACL, CertificateAuthorityACL_BouncyCastle>();
    }
    
    protected override void _BeforeBuild(WebApplication app, Options options)
    {
    }

    protected override void _AfterBuild(WebApplication app, Options options)
    {
    }

    protected override async Task _BeforeRun(WebApplication app, Options options)
    {
        var adminSeed = ActivatorUtilities.CreateInstance<AdminUserSeed>(app.Services);
        await adminSeed.Execute();
    }
}

namespace IAM.Identities.Service
{
    using MongoDB.Driver.Core.Configuration;
    using PolyPersist.Net.BlobStore.GridFS;
    using PolyPersist.Net.BlobStore.Memory;
    using PolyPersist.Net.ColumnStore.Cassandra;
    using PolyPersist.Net.ColumnStore.Memory;
    using PolyPersist.Net.Core;
    using PolyPersist.Net.DocumentStore.Memory;
    using PolyPersist.Net.DocumentStore.MongoDB;

    public class IdentityStoreProvider : StoreProvider
    {
        private readonly IConfiguration _configuration;

        public IdentityStoreProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override IDocumentStore GetDocumentStore() => new MongoDB_DocumentStore(_RequiredConnectionString("Stores:Document"));
        protected override IBlobStore GetBlobStore() => new GridFS_BlobStore(_RequiredConnectionString("Stores:Blob"));
        protected override IColumnStore GetColumnStore() => new Cassandra_ColumnStore(_RequiredConnectionString("Stores:Column"));

        // A connection string carries credentials, so it comes from configuration - in the cluster
        // from environment variables (Stores__Document and so on), fed from a secret. Missing means
        // stop: starting without a store would only fail later, somewhere far less obvious. The
        // value is never logged.
        private string _RequiredConnectionString(string key)
        {
            var connectionString = _configuration[key];
            if (string.IsNullOrWhiteSpace(connectionString) == true)
                throw new InvalidOperationException($"The connection string '{key}' is not configured. Set it in configuration, or as the '{key.Replace(":", "__")}' environment variable.");

            return connectionString;
        }
    }
}
