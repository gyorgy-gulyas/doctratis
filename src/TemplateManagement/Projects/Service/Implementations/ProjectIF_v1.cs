using ServiceKit.Net;
using TemplateManagement.Projects.Project;

namespace TemplateManagement.Projects.Service.Implementations
{
    public class ProjectIF_v1 : IProjectIF_v1
    {
        private readonly IProjectService _service;
        private readonly ILogger _logger;

        public ProjectIF_v1(IProjectService service, ILogger<ProjectIF_v1> logger)
        {
            _service = service;
            _logger = logger;
        }

        async Task<Response<IProjectIF_v1.ProjectSummaryDTO>> IProjectIF_v1.createProject(CallingContext ctx, string name, string description, string createdBy)
        {
            var header = await _service.createProject(ctx, name, description, createdBy);
            if (header.IsSuccess() == false)
                return new(header.Error);

            return new(header.Value.ConvertToSummaryDTO());
        }

        async Task<Response<List<IProjectIF_v1.ProjectSummaryDTO>>> IProjectIF_v1.listAccessibleProjects(CallingContext ctx)
        {
            var projects = await _service.getAllProjectForUser(ctx, ctx.ClientInfo.CallingUserId);
            if (projects.IsSuccess() == false)
                return new(projects.Error);

            return new(projects.Value.Select(ph => ph.ConvertToSummaryDTO()).ToList());
        }

        async Task<Response<List<IProjectIF_v1.ProjectSummaryDTO>>> IProjectIF_v1.listAccessibleProjectsForUser(CallingContext ctx, string urseId)
        {
            var projects = await _service.getAllProjectForUser(ctx, urseId);
            if (projects.IsSuccess() == false)
                return new(projects.Error);

            return new(projects.Value.Select(ph => ph.ConvertToSummaryDTO()).ToList());
        }

        async Task<Response<IProjectIF_v1.ProjectDetailsDTO>> IProjectIF_v1.getProject(CallingContext ctx, string projectId)
        {
            var project = await _service.getProjectForUser(ctx, projectId, ctx.ClientInfo.CallingUserId);
            if (project.IsSuccess() == false)
                return new(project.Error);

            var accesses = await _service.getAllAccessForProject(ctx, projectId);
            if (accesses.IsSuccess() == false)
                return new(accesses.Error);

            return new(project.Value.ConvertToDetailsDTO( accesses.Value ));
        }
    }

    internal static class ConversionExtensions
    {
        internal static IProjectIF_v1.ProjectStatuses Convert(this ProjectStatuses @this)
        {
            return @this switch
            {
                ProjectStatuses.Draft => IProjectIF_v1.ProjectStatuses.Draft,
                ProjectStatuses.Active => IProjectIF_v1.ProjectStatuses.Active,
                ProjectStatuses.Locked => IProjectIF_v1.ProjectStatuses.Locked,
                ProjectStatuses.Archived => IProjectIF_v1.ProjectStatuses.Archived,
                ProjectStatuses.Deleted => IProjectIF_v1.ProjectStatuses.Deleted,
                _ => throw new NotImplementedException(),
            };
        }

        internal static IProjectIF_v1.ProjectAccessRoles Convert(this ProjectAccess.Roles @this)
        {
            return @this switch
            {
                ProjectAccess.Roles.Reader => IProjectIF_v1.ProjectAccessRoles.Reader,
                ProjectAccess.Roles.Editor => IProjectIF_v1.ProjectAccessRoles.Editor,
                ProjectAccess.Roles.Owner => IProjectIF_v1.ProjectAccessRoles.Owner,
                ProjectAccess.Roles.Auditor => IProjectIF_v1.ProjectAccessRoles.Auditor,
                ProjectAccess.Roles.Admin => IProjectIF_v1.ProjectAccessRoles.Admin,
                _ => throw new NotImplementedException(),
            };
        }
        internal static IProjectIF_v1.ProjectAccessStatuses Convert(this ProjectAccess.Statuses @this)
        {
            return @this switch
            {
                ProjectAccess.Statuses.Pending => IProjectIF_v1.ProjectAccessStatuses.Pending,
                ProjectAccess.Statuses.Active => IProjectIF_v1.ProjectAccessStatuses.Active,
                ProjectAccess.Statuses.Suspended => IProjectIF_v1.ProjectAccessStatuses.Suspended,
                ProjectAccess.Statuses.Revoked => IProjectIF_v1.ProjectAccessStatuses.Revoked,
                ProjectAccess.Statuses.Deleted => IProjectIF_v1.ProjectAccessStatuses.Deleted,
                _ => throw new NotImplementedException(),
            };
        }

        internal static IProjectIF_v1.ProjectSummaryDTO ConvertToSummaryDTO(this ProjectHeader @this)
        {
            return new IProjectIF_v1.ProjectSummaryDTO()
            {
                id = @this.id,
                Name = @this.Name,
                Description = @this.Description,
                Status = @this.Status.Convert(),
                Tags = @this.Tags,
            };
        }

        internal static IProjectIF_v1.ProjectDetailsDTO ConvertToDetailsDTO(this ProjectHeader @this, IEnumerable<ProjectAccess> accesses)
        {
            return new IProjectIF_v1.ProjectDetailsDTO()
            {
                id = @this.id,
                Name = @this.Name,
                Description = @this.Description,
                Status = @this.Status.Convert(),
                Tags = @this.Tags,
                CreatedAt = @this.CreatedAt,
                CreatedBy = @this.CreatedBy,
                Accesses = accesses.Select(a => a.ConvertToDTO()).ToList()
            };
        }

        internal static IProjectIF_v1.ProjectAccessDTO  ConvertToDTO( this ProjectAccess  @this )
        {
            return new IProjectIF_v1.ProjectAccessDTO()
            {
                id = @this.id,
                IdentityId = @this.IdentityId,
                Role = @this.Role.Convert(),
                Status = @this.Status.Convert(),
            };
        }
    }
}
