using PolyPersist;

namespace TemplateManagement.Projects.Project
{
    public partial class ProjectHeader : IDocument
    {
        string IEntity.PartitionKey { get => id; set => id = value; }
    }
}
