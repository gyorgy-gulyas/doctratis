namespace IAM.Identities.Service.Implementations.Helpers
{
    internal static class PasswordRules
    {
        internal const int Hash_Iterations = 10_000;
        internal const int Hash_KeySize = 32; // 256 bit
    }
}
