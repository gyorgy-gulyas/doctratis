using Core.Auditing.Worker;
using PolyPersist;
using PolyPersist.Net.Core;
using SrvKit.Net;
using TemplateManagement.Projects;
using TemplateManagement.Projects.Service;
using TemplateManagement.Projects.Service.Implementations;

var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddAuthentication("Bearer");
//builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddSingleton<IStoreProvider>(new ProjectStoreProvider());
builder.Services.AddSingleton<ProjectStoreContext>();
builder.Services.AddAuditWorker();

builder.Services.AddSingleton<IProjectIF_v1,ProjectIF_v1>();
builder.Services.AddSingleton<IProjectService,ProjectService>();

var app = builder.Build();

//app.UseAuthentication();
//app.UseAuthorization();
//app.MapRestControllers();
app.MapControllers();
app.MapGrpcControllers();
app.MapGet("/", () => "Service is running!");

app.Run();

namespace TemplateManagement.Projects.Service
{
    using PolyPersist.Net.BlobStore.GridFS;
    using PolyPersist.Net.BlobStore.Memory;
    using PolyPersist.Net.DocumentStore.Memory;
    using PolyPersist.Net.DocumentStore.MongoDB;

    public class ProjectStoreProvider : StoreProvider
    {
//        protected override IDocumentStore GetDocumentStore() => new MongoDB_DocumentStore("mongodb://127.0.0.1:27617/?directConnection=true");
//        protected override IBlobStore GetBlobStore() => new GridFS_BlobStore("mongodb://127.0.0.1:27617/?directConnection=true");

        protected override IDocumentStore GetDocumentStore() => new Memory_DocumentStore("");
        protected override IBlobStore GetBlobStore() => new Memory_BlobStore("");
    }
}

// http://localhost:5000/templatemanagement/projects/projectif/v1/listaccessibleprojects