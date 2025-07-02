using PolyPersist.Net.Extensions;
using ServiceKit.Net;
using TemplateManagement.Projects.Project;

namespace TemplateManagement.Projects.Service.Implementations
{
    public class ProjectService : IProjectService
    {
        private readonly ProjectStoreContext _context;

        public ProjectService(ProjectStoreContext context)
        {
            _context = context;
        }

        async Task<Response<ProjectHeader>> IProjectService.createProject(CallingContext ctx, string name, string description, string createdBy)
        {
            name = name.Trim();

            bool already = _context
                .ProjectHeaders
                .AsQueryable()
                .Where(ph => ph.Name == name)
                .Any();
            if (already == true)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Project name '{name}'is already exist" });

            ProjectHeader header = new()
            {
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                Status = ProjectStatuses.Draft,
            };
            await _context.ProjectHeaders.Insert(header);

            ProjectAccess access = new()
            {
                IdentityId = createdBy,
                ProjectId = header.id,
                Role = ProjectAccess.Roles.Owner,
                Status = ProjectAccess.Statuses.Active,
            };
            await _context.ProjectAccesses.Insert(access);

            return new(header);
        }

        async Task<Response<List<ProjectHeader>>> IProjectService.getAllProjectForUser(CallingContext ctx, string userId)
        {
            var projectIds = _context
                .ProjectAccesses
                .AsQueryable()
                .Where(pa => pa.IdentityId == userId && pa.Status == ProjectAccess.Statuses.Active)
                .Select(pa => pa.ProjectId)
                .Distinct()
                .ToArray();

            var result = _context
                .ProjectHeaders
                .AsQueryable()
                .Where(ph => projectIds.Contains(ph.id))
                .ToList();

            return new(result);
        }

        async Task<Response<ProjectHeader>> IProjectService.getProjectForUser(CallingContext ctx, string projectId, string userId)
        {
            var project = await _context.ProjectHeaders.Find(projectId, projectId);
            if (project == null)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Project with '{projectId}'is not exist" });

            ProjectAccess access = _context
                .ProjectAccesses
                .AsQueryable()
                .Where(pa => pa.IdentityId == userId && pa.ProjectId == projectId)
                .FirstOrDefault();
            if (access == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"User with id '{userId}' does not have an access for project '{projectId}'" });

            return new(project);
        }

        async Task<Response<List<ProjectAccess>>> IProjectService.getAllAccessForProject(CallingContext ctx, string projectId)
        {
            var project = await _context.ProjectHeaders.Find(projectId, projectId);
            if (project == null)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Project with '{projectId}'is not exist" });

            var accesses = _context
                .ProjectAccesses
                .AsQueryable()
                .Where(pa => pa.ProjectId == projectId)
                .ToList();

            return new(accesses);
        }
    }
}
