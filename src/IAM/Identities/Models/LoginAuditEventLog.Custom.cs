using PolyPersist;

namespace IAM.Identities.Identity
{
	public partial class LoginAuditEventLog : IRow
	{
        string IEntity.PartitionKey { get => idenityId; set => idenityId = value; }
    }

}
