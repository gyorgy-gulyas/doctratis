using PolyPersist;

namespace IAM.Identities.Identity
{
    public partial class Auth : IDocument
    {
        public string PartitionKey { get => accountId; set => id = accountId; }
    }
}
