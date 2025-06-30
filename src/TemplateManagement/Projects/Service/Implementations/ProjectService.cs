using ServiceKit.Net;
using TemplateManagement.Projects.Project;

namespace TemplateManagement.Projects.Service.Implementations
{
    public class ProjectService : IProjectService
    {
        private readonly ProjectStoreContext _context;

        public ProjectService(ProjectStoreContext context )
        {
            _context = context;
        }

        async Task<ProjectHeader> IProjectService.createProject(CallingContext ctx, string name, string description, string createdBy)
        {
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

            return header;
        }

        Task<List<ProjectHeader>> IProjectService.getAllProjectForUser(CallingContext ctx, string userId)
        {
            throw new NotImplementedException();
        }
    }
}
