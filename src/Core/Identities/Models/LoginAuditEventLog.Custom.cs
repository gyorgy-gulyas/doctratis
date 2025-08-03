using PolyPersist;

namespace Core.Identities.Identity
{
	public partial class LoginAuditEventLog : IRow
	{
        string IEntity.PartitionKey { get => idenityId; set => idenityId = value; }
    }

}
