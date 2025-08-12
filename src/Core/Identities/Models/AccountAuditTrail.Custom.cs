using PolyPersist;

namespace Core.Identities.Identity
{
	public partial class AccountAuditTrail : IRow
	{
		string IEntity.PartitionKey { get => accountId; set => accountId = value; }
	}

}
