using System.Text.Json;
using Core.Auditing;
using Core.Auditing.Worker;
using PolyPersist;
using PolyPersist.Net.Context;
using PolyPersist.Net.Extensions;
using ServiceKit.Net;
using TemplateManagement.Projects.Project;

namespace TemplateManagement.Projects.Service.Implementations
{
    public class ProjectStoreContext : StoreContext
    {
        private readonly IAuditEntryContainer _auditEntryContainer;
        public readonly IDocumentCollection<ProjectHeader> ProjectHeaders;
        public readonly IDocumentCollection<ProjectAccess> ProjectAccesses;
        public readonly IColumnTable<ProjectAuditTrail> ProjectAuditTrails;

        public ProjectStoreContext(IStoreProvider storeProvider, IAuditEntryContainer auditEntryContainer)
            : base(storeProvider)
        {
            _auditEntryContainer = auditEntryContainer;

            ProjectHeaders = base.GetOrCreateDocumentCollection<ProjectHeader>().Result;
            ProjectAccesses = base.GetOrCreateDocumentCollection<ProjectAccess>().Result;
            ProjectAuditTrails = base.GetOrCreateColumnTable<ProjectAuditTrail>().Result;
        }

        internal void Audit(TrailOperations operation, CallingContext ctx, ProjectHeader header = null, IEnumerable<ProjectAccess> accesses = null, string projectId = null )
        {
            _auditEntryContainer.AddEntryForBackgrondSave(new ProjectAuditEntry(this, ctx, operation) {
                projectId = header != null ? header.id : projectId,
                header = header,
                accesses = accesses,
            });
        }
    }

    public class ProjectAuditEntry : AuditTrail<ProjectAuditTrail>
    {
        private readonly ProjectStoreContext _storeContext;
        public string projectId { get; set; }
        public ProjectHeader header { get; set; }
        public IEnumerable<ProjectAccess> accesses { get; set; }

        public ProjectAuditEntry(ProjectStoreContext storeContext, CallingContext callingContext, TrailOperations operation)
            : base(callingContext, operation)
        {
            _storeContext = storeContext;
        }

        protected override async Task InitializeAsync()
        {
            header ??= await _storeContext.ProjectHeaders.Find(projectId, projectId);
            accesses ??= _storeContext.ProjectAccesses
                    .AsQueryable()
                    .Where(pa => pa.ProjectId == projectId)
                    .ToArray();
        }

        protected override void FillAddtionalMembers(ProjectAuditTrail trail)
        {
            trail.projectId = projectId;
        }

        protected override IEntity GetRootEntity() => header;
        protected override string GetEntitySpecificPayloadJSON() => JsonSerializer.Serialize(new { header, accesses });
        protected override IColumnTable<ProjectAuditTrail> GetTable() => _storeContext.ProjectAuditTrails;
    }
}
