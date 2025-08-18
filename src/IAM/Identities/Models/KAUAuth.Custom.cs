using PolyPersist;
using PolyPersist.Net.Common;

namespace IAM.Identities.Identity
{
	/// KAU Ügyfélkapu authentication
	public partial class KAUAuth : IValidable
	{
        bool IValidable.Validate(IList<IValidationError> errors)
        {
            if (string.IsNullOrEmpty(KAUUserId) == true)
                errors.Add(new ValidationError()
                {
                    TypeOfEntity = this.GetType().Name,
                    MemberOfEntity = nameof(KAUUserId),
                    ErrorText = $"Member : '{nameof(KAUUserId)}' cannot be empty"
                });

            return errors.Count == 0;
        }
    }
}
