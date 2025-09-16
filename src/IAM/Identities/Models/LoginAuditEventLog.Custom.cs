using PolyPersist;

namespace IAM.Identities.Identity
{
	public partial class LoginAuditEventLog : IRow
	{
        string IEntity.PartitionKey { get => idenityId; set => idenityId = value; }
        public string PartitionKey
        {
            get => (this as IEntity).PartitionKey;
            private set => (this as IEntity).PartitionKey = value;
        }
    }

}
