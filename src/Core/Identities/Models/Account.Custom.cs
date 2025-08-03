using PolyPersist;

namespace Core.Identities.Identity
{
    public partial class Account : IDocument
    {
        string IEntity.PartitionKey { get => id; set => id = value; }
    }

}
