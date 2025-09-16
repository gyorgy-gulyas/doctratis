using PolyPersist;

namespace IAM.Identities.Identity
{
    public partial class Account : IDocument
    {
        string IEntity.PartitionKey { get => id; set => id = value; }
        public string PartitionKey { 
            get => (this as IEntity).PartitionKey;
            private set => (this as IEntity).PartitionKey = value;
        }
    }

}
