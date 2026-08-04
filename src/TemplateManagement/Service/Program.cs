using Core.Auditing.Worker;
using PolyPersist;
using ServiceKit.Net;
using TemplateManagement.Projects;
using TemplateManagement.Projects.Service;
using TemplateManagement.Projects.Service.Implementations;


BaseServiceHost.Create<TemplateManagementServiceHost>(args, new BaseServiceHost.Options()
{
    WithAuthentication = false,
    WithGrpc = true,
    WithRest = true,
    WithReponseCompression = false,
    PathBase = "/templatemanagement"
}).Run();

public class TemplateManagementServiceHost : BaseServiceHost
{
    protected override void _BeforeAddServices(IServiceCollection services, Options options)
    {
    }

    protected override void _AfterAddServices(IServiceCollection services, Options options)
    {
        services.AddSingleton<IStoreProvider>(provider => new ProjectStoreProvider(provider.GetRequiredService<IConfiguration>()));
        services.AddSingleton<ProjectStoreContext>();
        services.AddAuditWorker();

        services.AddSingleton<IProjectIF_v1, ProjectIF_v1>();
        services.AddSingleton<IProjectService, ProjectService>();
    }

    protected override void _BeforeBuild(WebApplication app, Options options)
    {
    }

    protected override void _AfterBuild(WebApplication app, Options options)
    {
    }

    protected override Task _BeforeRun(WebApplication app, Options options)
    {
        return Task.CompletedTask;
    }
}

namespace TemplateManagement.Projects.Service
{
    using PolyPersist.Net.BlobStore.GridFS;
    using PolyPersist.Net.BlobStore.Memory;
    using PolyPersist.Net.ColumnStore.Cassandra;
    using PolyPersist.Net.ColumnStore.Memory;
    using PolyPersist.Net.Core;
    using PolyPersist.Net.DocumentStore.Memory;
    using PolyPersist.Net.DocumentStore.MongoDB;

    public class ProjectStoreProvider : StoreProvider
    {
        private readonly IConfiguration _configuration;

        public ProjectStoreProvider(IConfiguration configuration)
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
