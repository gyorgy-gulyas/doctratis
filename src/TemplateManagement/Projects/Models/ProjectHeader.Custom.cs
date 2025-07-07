using PolyPersist;

namespace TemplateManagement.Projects.Project
{
    public partial class ProjectHeader : IDocument
    {
        string IEntity.PartitionKey { get => id; set => id = value; }

        public bool IsEditable()
        {
            return Status switch
            {
                ProjectStatuses.Draft => true,
                ProjectStatuses.Active => true,
                _ => false
            };
        }

        private static List<ProjectAccess.Roles> _allowedRoles = [ProjectAccess.Roles.Admin, ProjectAccess.Roles.Editor, ProjectAccess.Roles.Owner];
        private static List<ProjectAccess.Statuses> _allowedStatuses = [ProjectAccess.Statuses.Active];
        public bool CanAccessEdit(ProjectAccess access)
        {
            if (_allowedRoles.Contains(access.Role) == false)
                return false;

            if (_allowedStatuses.Contains(access.Status) == false)
                return false;

            return true;
        }
    }
}
