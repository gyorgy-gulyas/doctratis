using PolyPersist;
using PolyPersist.Net.Common;

namespace IAM.Identities.Identity
{
	public partial class ADAuth : IValidable
	{
        bool IValidable.Validate(IList<IValidationError> errors)
        {
            if (string.IsNullOrEmpty(LdapDomainId) == true)
                errors.Add(new ValidationError()
                {
                    TypeOfEntity = this.GetType().Name,
                    MemberOfEntity = nameof(LdapDomainId),
                    ErrorText = $"Member : '{nameof(LdapDomainId)}' cannot be empty"
                });

            if (string.IsNullOrEmpty(userName) == true)
                errors.Add(new ValidationError()
                {
                    TypeOfEntity = this.GetType().Name,
                    MemberOfEntity = nameof(userName),
                    ErrorText = $"Member : '{nameof(userName)}' cannot be empty"
                });

            return errors.Count == 0;
        }
    }
}
