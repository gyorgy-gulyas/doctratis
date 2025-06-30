using PolyPersist;
using PolyPersist.Net.BlobStore.GridFS;
using PolyPersist.Net.Core;
using PolyPersist.Net.DocumentStore.MongoDB;
using SrvKit.Net;
using TemplateManagement.Projects.Service;
using TemplateManagement.Projects.Service.Implementations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddSingleton<IStoreProvider>(new ProjectStoreProvider());
builder.Services.AddSingleton<ProjectStoreContext>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapRestControllers();
app.MapGrpcControllers();

app.Run();

namespace TemplateManagement.Projects.Service
{
    public class ProjectStoreProvider : StoreProvider
    {
        protected override IDocumentStore GetDocumentStore() => new MongoDB_DocumentStore("mongodb://127.0.0.1:27617/?directConnection=true");
        protected override IBlobStore GetBlobStore() => new GridFS_BlobStore("mongodb://127.0.0.1:27617/?directConnection=true");
    }
}