using PolyPersist;

namespace IAM.Identities.Identity
{
	public partial class AccountAuditTrail : IRow
	{
		string IEntity.PartitionKey { get => accountId; set => accountId = value; }
        public string PartitionKey
        {
            get => (this as IEntity).PartitionKey;
            private set => (this as IEntity).PartitionKey = value;
        }
    }
}


