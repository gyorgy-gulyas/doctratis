using PolyPersist;

namespace Core.Identities.Ldap
{
	public partial class LdapDomain : IDocument
	{
        string IEntity.PartitionKey { get => id; set => id = value; }
    }

}
