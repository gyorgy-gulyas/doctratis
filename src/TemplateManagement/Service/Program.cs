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
        services.AddSingleton<IStoreProvider>(new ProjectStoreProvider());
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
        protected override IDocumentStore GetDocumentStore() => new MongoDB_DocumentStore("mongodb://docratis:DocratisMongoPassword@mongodb.docratis-store.svc.cluster.local:27017/docratis_documents?directConnection=true");
        protected override IBlobStore GetBlobStore() => new GridFS_BlobStore("mongodb://docratis:DocratisMongoPassword@mongodb.docratis-store.svc.cluster.local:27017/docratis_documents?directConnection=true");
        protected override IColumnStore GetColumnStore() => new Cassandra_ColumnStore("host=scylla-client.docratis-store.svc.cluster.local;port=9042;username=docratis;password=DocratisScyllaPassword;keyspace=docratis");

        //protected override IDocumentStore GetDocumentStore() => new Memory_DocumentStore("");
        //protected override IBlobStore GetBlobStore() => new Memory_BlobStore("");
        //protected override IColumnStore GetColumnStore() => new Memory_ColumnStore("");
    }
}
