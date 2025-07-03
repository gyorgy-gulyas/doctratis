using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading.Channels;
using Core.Auditing;
using PolyPersist;
using PolyPersist.Net.Context;
using PolyPersist.Net.Extensions;
using ServiceKit.Net;
using TemplateManagement.Projects.Project;

namespace TemplateManagement.Projects.Service.Implementations
{
    public class ProjectStoreContext : StoreContext
    {
        public readonly IDocumentCollection<ProjectHeader> ProjectHeaders;
        public readonly IDocumentCollection<ProjectAccess> ProjectAccesses;
        public readonly IColumnTable<ProjectAuditTrail> ProjectAuditTrails;

        public ProjectStoreContext(IStoreProvider storeProvider)
            : base(storeProvider)
        {
            IDocumentStore documentStore = (IDocumentStore)storeProvider.getStore(IStore.StorageModels.Document);

            ProjectHeaders = documentStore.GetCollectionByName<ProjectHeader>(nameof(ProjectHeader)).Result;
            if (ProjectHeaders == null)
                ProjectHeaders = documentStore.CreateCollection<ProjectHeader>(nameof(ProjectHeader)).Result;

            ProjectAccesses = documentStore.GetCollectionByName<ProjectAccess>(nameof(ProjectAccess)).Result;
            if (ProjectAccesses == null)
                ProjectAccesses = documentStore.CreateCollection<ProjectAccess>(nameof(ProjectAccess)).Result;

            IColumnStore columnStore = (IColumnStore)storeProvider.getStore(IStore.StorageModels.ColumnStore);
            ProjectAuditTrails = columnStore.GetTableByName<ProjectAuditTrail>(nameof(ProjectAuditTrail)).Result;
            if (ProjectAuditTrails == null)
                ProjectAuditTrails = columnStore.CreateTable<ProjectAuditTrail>(nameof(ProjectAuditTrail)).Result;
        }

        internal void EnqueueProjectAudit(CallingContext ctx, TrailOperations operation, ProjectHeader header, IEnumerable<ProjectAccess> accesses)
        {
            _EnqueueProjectAuditAsync(ctx, operation, header, accesses).SafeFireAndForget();
        }
        internal void EnqueueProjectAudit(CallingContext ctx, TrailOperations operation, ProjectHeader header )
        {
            _EnqueueProjectAuditAsync(ctx, operation, header).SafeFireAndForget();
        }
        internal void EnqueueProjectAudit(CallingContext ctx, TrailOperations operation, string projectId)
        {
            _EnqueueProjectAuditAsync(ctx, operation, projectId).SafeFireAndForget();
        }

        internal async Task _EnqueueProjectAuditAsync(CallingContext ctx, TrailOperations operation, ProjectHeader header, IEnumerable<ProjectAccess> accesses)
        {
            ProjectAuditTrail trail = new()
            {
                entityType = "Project",
                entityId = header.id,
                PartitionKey = header.id,
                payload = JsonSerializer.Serialize(new { header, accesses }),
                timestamp = DateTime.UtcNow,
                trailOperation = operation,
                userId = ctx.ClientInfo.CallingUserId,
                userName = ctx.ClientInfo.CallingUserId,
            };

            await ProjectAuditTrails.Insert(trail);
        }

        private async Task _EnqueueProjectAuditAsync(CallingContext ctx, TrailOperations operation, string projectId)
        {
            var header = await ProjectHeaders.Find(projectId, projectId);
            var accesses = ProjectAccesses
                .AsQueryable()
                .Where(pa => pa.ProjectId == projectId);

            await _EnqueueProjectAuditAsync(ctx, operation, header, accesses);
        }
        
        private async Task _EnqueueProjectAuditAsync(CallingContext ctx, TrailOperations operation, ProjectHeader header)
        {
            var accesses = ProjectAccesses
                .AsQueryable()
                .Where(pa => pa.ProjectId == header.id);

            await _EnqueueProjectAuditAsync(ctx, operation, header, accesses);
        }
    }
    
    public static class TaskExtensions
{
    public static void SafeFireAndForget(
        this Task task,
        bool continueOnCapturedContext = false,
        Action<Exception> onException = null)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        async void FireAndForgetWrapper()
        {
            try
            {
                await task.ConfigureAwait(continueOnCapturedContext);
            }
            catch (Exception ex)
            {
                // Logolhatod vagy callback-et hívhatsz
                onException?.Invoke(ex);
                // Vagy globális logger:
                // Logger.LogError(ex, "SafeFireAndForget error");
            }
        }

        FireAndForgetWrapper();
    }
}
}
