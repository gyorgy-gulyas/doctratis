using PolyPersist;

namespace IAM.Identities.Ldap
{
	public partial class LdapDomain : IDocument
	{
        string IEntity.PartitionKey { get => id; set => id = value; }
        public string PartitionKey { get => (this as IEntity).PartitionKey; }
    }

}
