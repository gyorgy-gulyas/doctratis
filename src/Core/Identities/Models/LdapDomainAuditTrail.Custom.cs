using PolyPersist;

namespace Core.Identities.Ldap
{
	public partial class LdapDomainAuditTrail : IRow
	{
		string IEntity.PartitionKey { get => domainId; set => domainId = value; }
	}
}
