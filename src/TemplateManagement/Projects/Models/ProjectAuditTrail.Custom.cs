using PolyPersist;
using PolyPersist.Net.Attributes;

namespace TemplateManagement.Projects.Project
{
	public partial class ProjectAuditTrail : IRow
	{
		string IEntity.PartitionKey { get => entityId; set => entityId = value; }
	}

}
