using PolyPersist;

namespace IAM.Identities.Identity
{
    public partial class Auth : IDocument
    {
        string IEntity.PartitionKey { get => accountId; set => id = accountId; }
    }
}
