using PolyPersist;
using PolyPersist.Net.Common;

namespace IAM.Identities.Ldap
{
	public partial class LdapDomain : IDocument, IValidable
	{
        string IEntity.PartitionKey { get => id; set => id = value; }
        public string PartitionKey
        {
            get => (this as IEntity).PartitionKey;
            private set => (this as IEntity).PartitionKey = value;
        }


        bool IValidable.Validate(IList<IValidationError> errors)
        {
            if (string.IsNullOrEmpty(name) == true)
                errors.Add(new ValidationError()
                {
                    TypeOfEntity = this.GetType().Name,
                    MemberOfEntity = nameof(name),
                    ErrorText = $"Member : '{nameof(name)}' cannot be empty"
                });

            return errors.Count == 0;
        }
    }
}
