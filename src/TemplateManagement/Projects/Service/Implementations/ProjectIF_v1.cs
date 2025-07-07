using ServiceKit.Net;

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
            var header = await _service.createProject(ctx, name, description, createdBy).ConfigureAwait(false);
            if (header.IsSuccess() == false)
                return new(header.Error);

            return new(header.Value.ConvertToSummaryDTO());
        }
        async Task<Response<IProjectIF_v1.ProjectDetailsDTO>> IProjectIF_v1.updateProject(CallingContext ctx, IProjectIF_v1.ProjectDetailsDTO project)
        {
            var header = await _service.updateProject(ctx, project.ConvertToHeader()).ConfigureAwait(false);
            if (header.IsSuccess() == false)
                return new(header.Error);

            List<Project.ProjectAccess> accesses = new();
            foreach (var dto in project.SubFolders)
            {
                var access = await _service.updateProjectAccess(ctx, dto.Convert()).ConfigureAwait(false);
                if (access.IsSuccess() == false)
                    return new(access.Error);

                accesses.Add(access.Value);
            }
            
            return new(header.Value.ConvertToDetailsDTO(accesses));
        }

        async Task<Response<List<IProjectIF_v1.ProjectSummaryDTO>>> IProjectIF_v1.listAccessibleProjects(CallingContext ctx)
        {
            var projects = await _service.getAllProjectForUser(ctx, ctx.ClientInfo.CallingUserId).ConfigureAwait(false);
            if (projects.IsSuccess() == false)
                return new(projects.Error);

            return new(projects.Value.Select(ph => ph.ConvertToSummaryDTO()).ToList());
        }

        async Task<Response<List<IProjectIF_v1.ProjectSummaryDTO>>> IProjectIF_v1.listAccessibleProjectsForUser(CallingContext ctx, string urseId)
        {
            var projects = await _service.getAllProjectForUser(ctx, urseId).ConfigureAwait(false);
            if (projects.IsSuccess() == false)
                return new(projects.Error);

            return new(projects.Value.Select(ph => ph.ConvertToSummaryDTO()).ToList());
        }

        async Task<Response<IProjectIF_v1.ProjectDetailsDTO>> IProjectIF_v1.getProject(CallingContext ctx, string projectId)
        {
            var project = await _service.getProjectForUser(ctx, projectId, ctx.ClientInfo.CallingUserId).ConfigureAwait(false);
            if (project.IsSuccess() == false)
                return new(project.Error);

            var accesses = await _service.getAllAccessForProject(ctx, projectId).ConfigureAwait(false);
            if (accesses.IsSuccess() == false)
                return new(accesses.Error);

            return new(project.Value.ConvertToDetailsDTO(accesses.Value));
        }

        async Task<Response<IProjectIF_v1.ProjectAccessDTO>> IProjectIF_v1.addProjectAccess(CallingContext ctx, string projectId, string identityId, IProjectIF_v1.ProjectAccessRoles role)
        {
            var access = await _service.addProjectAccess(ctx, projectId, identityId, role.Convert()).ConfigureAwait(false);
            if (access.IsSuccess() == false)
                return new(access.Error);

            return new(access.Value.ConvertToDTO());
        }
    }

    internal static class ConversionExtensions
    {
        internal static IProjectIF_v1.ProjectStatuses Convert(this Project.ProjectStatuses @this)
        {
            return @this switch
            {
                Project.ProjectStatuses.Draft => IProjectIF_v1.ProjectStatuses.Draft,
                Project.ProjectStatuses.Active => IProjectIF_v1.ProjectStatuses.Active,
                Project.ProjectStatuses.Locked => IProjectIF_v1.ProjectStatuses.Locked,
                Project.ProjectStatuses.Archived => IProjectIF_v1.ProjectStatuses.Archived,
                Project.ProjectStatuses.Deleted => IProjectIF_v1.ProjectStatuses.Deleted,
                _ => throw new NotImplementedException(),
            };
        }
        internal static Project.ProjectStatuses Convert(this IProjectIF_v1.ProjectStatuses @this)
        {
            return @this switch
            {
                IProjectIF_v1.ProjectStatuses.Draft => Project.ProjectStatuses.Draft,
                IProjectIF_v1.ProjectStatuses.Active => Project.ProjectStatuses.Active,
                IProjectIF_v1.ProjectStatuses.Locked => Project.ProjectStatuses.Locked,
                IProjectIF_v1.ProjectStatuses.Archived => Project.ProjectStatuses.Archived,
                IProjectIF_v1.ProjectStatuses.Deleted => Project.ProjectStatuses.Deleted,
                _ => throw new NotImplementedException(),
            };
        }

        internal static IProjectIF_v1.ProjectAccessRoles Convert(this Project.ProjectAccess.Roles @this)
        {
            return @this switch
            {
                Project.ProjectAccess.Roles.Reader => IProjectIF_v1.ProjectAccessRoles.Reader,
                Project.ProjectAccess.Roles.Editor => IProjectIF_v1.ProjectAccessRoles.Editor,
                Project.ProjectAccess.Roles.Owner => IProjectIF_v1.ProjectAccessRoles.Owner,
                Project.ProjectAccess.Roles.Auditor => IProjectIF_v1.ProjectAccessRoles.Auditor,
                Project.ProjectAccess.Roles.Admin => IProjectIF_v1.ProjectAccessRoles.Admin,
                _ => throw new NotImplementedException(),
            };
        }

        internal static Project.ProjectAccess.Roles Convert(this IProjectIF_v1.ProjectAccessRoles @this)
        {
            return @this switch
            {
                IProjectIF_v1.ProjectAccessRoles.Reader => Project.ProjectAccess.Roles.Reader,
                IProjectIF_v1.ProjectAccessRoles.Editor => Project.ProjectAccess.Roles.Editor,
                IProjectIF_v1.ProjectAccessRoles.Owner => Project.ProjectAccess.Roles.Owner,
                IProjectIF_v1.ProjectAccessRoles.Auditor => Project.ProjectAccess.Roles.Auditor,
                IProjectIF_v1.ProjectAccessRoles.Admin => Project.ProjectAccess.Roles.Admin,
                _ => throw new NotImplementedException(),
            };
        }

        internal static IProjectIF_v1.ProjectAccessStatuses Convert(this Project.ProjectAccess.Statuses @this)
        {
            return @this switch
            {
                Project.ProjectAccess.Statuses.Pending => IProjectIF_v1.ProjectAccessStatuses.Pending,
                Project.ProjectAccess.Statuses.Active => IProjectIF_v1.ProjectAccessStatuses.Active,
                Project.ProjectAccess.Statuses.Suspended => IProjectIF_v1.ProjectAccessStatuses.Suspended,
                Project.ProjectAccess.Statuses.Revoked => IProjectIF_v1.ProjectAccessStatuses.Revoked,
                Project.ProjectAccess.Statuses.Deleted => IProjectIF_v1.ProjectAccessStatuses.Deleted,
                _ => throw new NotImplementedException(),
            };
        }

        internal static Project.ProjectAccess.Statuses Convert(this IProjectIF_v1.ProjectAccessStatuses @this)
        {
            return @this switch
            {
                IProjectIF_v1.ProjectAccessStatuses.Pending => Project.ProjectAccess.Statuses.Pending,
                IProjectIF_v1.ProjectAccessStatuses.Active => Project.ProjectAccess.Statuses.Active,
                IProjectIF_v1.ProjectAccessStatuses.Suspended => Project.ProjectAccess.Statuses.Suspended,
                IProjectIF_v1.ProjectAccessStatuses.Revoked => Project.ProjectAccess.Statuses.Revoked,
                IProjectIF_v1.ProjectAccessStatuses.Deleted => Project.ProjectAccess.Statuses.Deleted,
                _ => throw new NotImplementedException(),
            };
        }

        internal static IProjectIF_v1.ProjectSummaryDTO ConvertToSummaryDTO(this Project.ProjectHeader @this)
        {
            return new()
            {
                id = @this.id,
                Name = @this.Name,
                Description = @this.Description,
                Status = @this.Status.Convert(),
                Tags = @this.Tags,
            };
        }

        internal static IProjectIF_v1.ProjectDetailsDTO ConvertToDetailsDTO(this Project.ProjectHeader @this, IEnumerable<Project.ProjectAccess> accesses)
        {
            return new()
            {
                id = @this.id,
                etag = @this.etag,
                Name = @this.Name,
                Description = @this.Description,
                SubFolders = @this.SubFolders.Convert(),
                Status = @this.Status.Convert(),
                Tags = @this.Tags,
                CreatedAt = @this.CreatedAt,
                CreatedBy = @this.CreatedBy,
                Accesses = accesses.Select(a => a.ConvertToDTO()).ToList()
            };
        }

        internal static List<IProjectIF_v1.ProjectFolderDTO> Convert(this List<Project.ProjectFolder> @this)
        {
            return @this.Select(f => f.Convert()).ToList();
        }

        internal static IProjectIF_v1.ProjectFolderDTO Convert(this Project.ProjectFolder @this)
        {
            return new()
            {
                id = @this.id,
                Name = @this.Name,
                Description = @this.Description,
                SubFolders = @this.SubFolders.Convert(),
            };
        }

        internal static Project.ProjectHeader ConvertToHeader(this IProjectIF_v1.ProjectDetailsDTO @this)
        {
            return new()
            {
                id = @this.id,
                etag = @this.etag,
                Name = @this.Name,
            };
        }


        internal static IProjectIF_v1.ProjectAccessDTO ConvertToDTO(this Project.ProjectAccess @this)
        {
            return new()
            {
                id = @this.id,
                IdentityId = @this.IdentityId,
                Role = @this.Role.Convert(),
                Status = @this.Status.Convert(),
            };
        }
        
        internal static Project.ProjectAccess ConvertToSummaryDTO(this IProjectIF_v1.ProjectAccessDTO @this)
        {
            return new()
            {
                id = @this.id,
                id = @this.et,
            };
        }

    }
}
