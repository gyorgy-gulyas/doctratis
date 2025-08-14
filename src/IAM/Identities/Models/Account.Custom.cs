using PolyPersist;

namespace IAM.Identities.Identity
{
    public partial class Account : IDocument
    {
        string IEntity.PartitionKey { get => id; set => id = value; }
    }

}
