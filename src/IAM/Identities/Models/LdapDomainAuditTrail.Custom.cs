using PolyPersist;

namespace IAM.Identities.Ldap
{
	public partial class LdapDomainAuditTrail : IRow
	{
		string IEntity.PartitionKey { get => domainId; set => domainId = value; }
        public string PartitionKey
        {
            get => (this as IEntity).PartitionKey;
            private set => (this as IEntity).PartitionKey = value;
        }
    }
}
