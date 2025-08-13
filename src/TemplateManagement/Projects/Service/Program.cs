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

/*
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
// Swagger service registartion
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// and service specific services
builder.Services.AddSingleton<IProjectIF_v1,ProjectIF_v1>();
builder.Services.AddSingleton<IProjectService,ProjectService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowReactApp"); // Middleware engedélyezés

// Swagger middleware bekapcsolása (csak dev-ben)
app.UseSwagger();
app.UseSwaggerUI();

//app.UseAuthentication();
//app.UseAuthorization();
//app.MapRestControllers();
app.MapControllers();
app.MapGrpcControllers();
app.MapGet("/", () => "Service is running!");

app.Run();
*/



// http://localhost:5000/templatemanagement/projects/projectif/v1/listaccessibleprojects
