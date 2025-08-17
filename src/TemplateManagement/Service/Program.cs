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

    protected override void _BeforeBuild(IServiceCollection services, Options options)
    {
    }

    protected override void _AfterBuild(IServiceCollection services, Options options)
    {
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
        //        protected override IDocumentStore GetDocumentStore() => new MongoDB_DocumentStore("mongodb://127.0.0.1:27617/?directConnection=true");
        //        protected override IBlobStore GetBlobStore() => new GridFS_BlobStore("mongodb://127.0.0.1:27617/?directConnection=true");

        protected override IDocumentStore GetDocumentStore() => new Memory_DocumentStore("");
        protected override IBlobStore GetBlobStore() => new Memory_BlobStore("");

        protected override IColumnStore GetColumnStore() => new Memory_ColumnStore("");
    }
}
