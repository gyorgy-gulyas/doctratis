using Core.Auditing.Worker;
using Core.Identities;
using Core.Identities.Service;
using Core.Identities.Service.Implementations;
using PolyPersist;
using PolyPersist.Net.Core;
using SrvKit.Net;


var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddAuthentication("Bearer");
//builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddSingleton<IStoreProvider>(new IdentityStoreProvider());
builder.Services.AddSingleton<IdentityStoreContext>();
builder.Services.AddAuditWorker();
// Swagger service registartion
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// and service specific services
builder.Services.AddSingleton<ILoginIF_v1, LoginIF_v1>();
builder.Services.AddSingleton<IAccountService, AccountService>();
builder.Services.AddSingleton<ILoginService, LoginService>();
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

namespace Core.Identities.Service
{
    using PolyPersist.Net.BlobStore.GridFS;
    using PolyPersist.Net.BlobStore.Memory;
    using PolyPersist.Net.ColumnStore.Cassandra;
    using PolyPersist.Net.ColumnStore.Memory;
    using PolyPersist.Net.DocumentStore.Memory;
    using PolyPersist.Net.DocumentStore.MongoDB;

    public class IdentityStoreProvider : StoreProvider
    {
        //        protected override IDocumentStore GetDocumentStore() => new MongoDB_DocumentStore("mongodb://127.0.0.1:27617/?directConnection=true");
        //        protected override IBlobStore GetBlobStore() => new GridFS_BlobStore("mongodb://127.0.0.1:27617/?directConnection=true");

        protected override IDocumentStore GetDocumentStore() => new Memory_DocumentStore("");
        protected override IBlobStore GetBlobStore() => new Memory_BlobStore("");
        protected override IColumnStore GetColumnStore() => new Memory_ColumnStore("");
    }
}
