using PolyPersist.Net.Extensions;
using PolyPersist.Net.Transactions;
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

        async Task<Response<ProjectHeader>> IProjectService.createProject(CallingContext ctx, string name, string description, string createdById, string createdByName)
        {
            name = name.Trim();

            bool already = _context
                .ProjectHeaders
                .AsQueryable()
                .Where(ph => ph.Name == name)
                .Any();
            if (already == true)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Project name '{name}'is already exist" });

            await using var tx = new Transaction();
            ProjectHeader header = new()
            {
                id = Guid.NewGuid().ToString(),
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                CreatedById = createdById,
                CreatedByName = createdByName,
                Status = ProjectStatuses.Draft,
            };
            await tx.Insert(_context.ProjectHeaders, header).ConfigureAwait(false);

            ProjectAccess access = new()
            {
                IdentityId = createdById,
                IdentityName = createdByName,
                ProjectId = header.id,
                Role = ProjectAccess.Roles.Owner,
                Status = ProjectAccess.Statuses.Active,
            };
            await tx.Insert(_context.ProjectAccesses, access).ConfigureAwait(false);
            await tx.Commit();

            _context.Audit(Core.Auditing.TrailOperations.Create, ctx, header, [access]);

            return new(header);
        }

        async Task<Response<ProjectHeader>> IProjectService.updateProject(CallingContext ctx, ProjectHeader project, IList<ProjectAccess> accesses, string  )
        {
            var original = await _context.ProjectHeaders.Find(project.id, project.id).ConfigureAwait(false);
            if (original == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Project '{project.id}' does not exist" });
            if (original.IsEditable() == false)
                return new(new Error() { Status = Statuses.Unauthorized, MessageText = $"The project '{original.id}' is not editable with status: '{original.Status}'" });

            ProjectAccess currentAccess = _context
                .ProjectAccesses
                .AsQueryable()
                .Where(pa => pa.IdentityId == ctx.IdentityId && pa.ProjectId == project.id)
                .FirstOrDefault();
            if (currentAccess == null)
                return new(new Error() { Status = Statuses.Unauthorized, MessageText = $"User with id '{ctx.ClientInfo.CallingUserId}' does not have an access for project '{project.id}'" });
            if (original.CanAccessEdit(currentAccess))
                return new(new Error() { Status = Statuses.Unauthorized, MessageText = $"User with id '{ctx.ClientInfo.CallingUserId}' does not have an permission to edit the project '{project.id}'" });

            await using var tx = new Transaction();
            await tx.Update(_context.ProjectHeaders, project).ConfigureAwait(false);

            var originalAccessesById = _context
                .ProjectAccesses
                .AsQueryable()
                .Where(pa => pa.ProjectId == project.id)
                .ToDictionary( a => a.id );

            var toInsert = new List<ProjectAccess>();
            var toUpdate = new List<ProjectAccess>();
            var toDelete = new List<ProjectAccess>(originalAccessesById.Values);
            
            foreach (var modified in accesses)
            {
                if (string.IsNullOrEmpty(modified.id) == true)
                {
                    toInsert.Add(modified);
                }
                else if (originalAccessesById.TryGetValue(modified.id, out var originalAccess))
                {
                    toDelete.Remove(originalAccess);

                    if (originalAccess.Equals(modified) == false)
                        toUpdate.Add(modified);
                }
                else // has id, but not in the list
                {
                    toInsert.Add(modified);
                }
            }

            var operations =
                    toInsert.Select(insert => tx.Insert(_context.ProjectAccesses, insert))
                .Concat(
                    toUpdate.Select(update => tx.Update(_context.ProjectAccesses, update)))
                .Concat(
                    toDelete.Select(delete => tx.Delete(_context.ProjectAccesses, delete )));

            if(operations.Any())
                await Task.WhenAll(operations);

            _context.Audit(Core.Auditing.TrailOperations.Update, ctx, project, accesses );

            return new(project);
        }
        Task<Response<List<ProjectHeader>>> IProjectService.getAllProjectForUser(CallingContext ctx, string userId)
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

            return Task.FromResult<Response<List<ProjectHeader>>>(new(result));
        }

        async Task<Response<ProjectHeader>> IProjectService.getProjectForIdentity(CallingContext ctx, string projectId, string identityId)
        {
            var project = await _context.ProjectHeaders.Find(projectId, projectId).ConfigureAwait(false);
            if (project == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Project '{projectId}' does not exist" });

            ProjectAccess access = _context
                .ProjectAccesses
                .AsQueryable()
                .Where(pa => pa.IdentityId == identityId && pa.ProjectId == projectId)
                .FirstOrDefault();
            if (access == null)
                return new(new Error() { Status = Statuses.Unauthorized, MessageText = $"User with id '{identityId}' does not have an access for project '{projectId}'" });

            return new(project);
        }

        async Task<Response<List<ProjectAccess>>> IProjectService.getAllAccessForProject(CallingContext ctx, string projectId)
        {
            var project = await _context.ProjectHeaders.Find(projectId, projectId).ConfigureAwait(false);
            if (project == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Project '{projectId}'does not exist" });

            var accesses = _context
                .ProjectAccesses
                .AsQueryable()
                .Where(pa => pa.ProjectId == projectId)
                .ToList();

            return new(accesses);
        }

        async Task<Response<ProjectAccess>> IProjectService.addProjectAccess(CallingContext ctx, string projectId, string identityId, ProjectAccess.Roles role)
        {
            var project = await _context.ProjectHeaders.Find(projectId, projectId).ConfigureAwait(false);
            if (project == null)
                return new(new Error() { Status = Statuses.NotFound, MessageText = $"Project '{projectId}' does not exist" });
            if (project.IsEditable() == false)
                return new(new Error() { Status = Statuses.Unauthorized, MessageText = $"The project '{projectId}' is not editable with status: '{project.Status}'" });

            var access = _context
                .ProjectAccesses
                .AsQueryable()
                .Where(pa => pa.ProjectId == projectId && pa.IdentityId == ctx.ClientInfo.CallingUserId)
                .FirstOrDefault();
            if (access == null)
                return new(new Error() { Status = Statuses.Unauthorized, MessageText = $"Current identity '{identityId}' does not have an access fot project: '{projectId}'" });

            if (project.CanAccessEdit(access))
                return new(new Error() { Status = Statuses.Unauthorized, MessageText = $"Current identity '{identityId}' does not have an permission to add other access for project: '{projectId}'" });

            var already = _context
                .ProjectAccesses
                .AsQueryable()
                .Where(pa => pa.ProjectId == projectId && pa.IdentityId == identityId)
                .FirstOrDefault();
            if (already != null)
                return new(new Error() { Status = Statuses.BadRequest, MessageText = $"Identity '{identityId}' is already added to project: '{projectId}' " });

            ProjectAccess newAccess = new()
            {
                IdentityId = identityId,
                ProjectId = project.id,
                Role = role,
                Status = ProjectAccess.Statuses.Active,
            };
            await _context.ProjectAccesses.Insert(newAccess).ConfigureAwait(false);

            _context.Audit(Core.Auditing.TrailOperations.Update, ctx, project );

            return new(access);
        }
    }
}
