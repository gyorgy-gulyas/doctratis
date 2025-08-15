namespace IAM.Identities.Service.Implementations.Helpers
{
    internal static class PasswordRules
    {
        // === Hashing parameters ===
        internal const int Hash_Iterations = 10_000;   // PBKDF2 iterations
        internal const int Hash_KeySize = 32;       // 256-bit derived key length
        internal const int Salt_Length = 16;       // 128-bit random salt size

        // === Password history ===
        internal const int History_MaxCount = 12;      // Store last N password hashes to prevent reuse

        // === Expiration ===
        internal const int ExpirationDays = 30;        // Default password expiration in days

        // === Password complexity rules ===
        internal const int MinLength = 12;
        internal const int MaxLength = 128;

        internal const int MinUppercase = 1;     // Minimum uppercase letters required
        internal const int MinLowercase = 1;     // Minimum lowercase letters required
        internal const int MinDigits = 1;     // Minimum digits required
        internal const int MinNonAlphanumeric = 1;     // Minimum special characters required

        internal const int MinUniqueChars = 5;    // Require at least N distinct characters
        internal const int MaxRepeatRun = 3;    // Same character can repeat max N times in a row

        /// <summary>
        /// Validates the given password against all rules.
        /// Returns a list of human-readable error messages.
        /// If the list is empty, the password is valid.
        /// </summary>
        internal static IReadOnlyList<string> Validate(string password, string accountName = null, string email = null)
        {
            var errors = new List<string>();

            if (password is null)
            {
                errors.Add("Password must not be null.");
                return errors;
            }

            // Length checks
            if (password.Length < MinLength)
                errors.Add($"Password must be at least {MinLength} characters long.");
            if (password.Length > MaxLength)
                errors.Add($"Password must be at most {MaxLength} characters long.");

            // Whitespace check
            if (password.Any(char.IsWhiteSpace))
                errors.Add("Password must not contain whitespace characters.");

            // Character class counters
            int upper = 0, lower = 0, digits = 0, nonAlnum = 0, unique = 0, maxRun = 1;
            var seen = new HashSet<char>();
            char? last = null; int run = 1;

            foreach (var ch in password)
            {
                if (char.IsUpper(ch)) upper++;
                else if (char.IsLower(ch)) lower++;
                else if (char.IsDigit(ch)) digits++;
                else nonAlnum++;

                if (seen.Add(ch)) unique++;

                if (last.HasValue && last.Value == ch)
                {
                    run++;
                    if (run > maxRun) maxRun = run;
                }
                else
                {
                    run = 1;
                    last = ch;
                }
            }

            if (upper < MinUppercase) errors.Add($"Password must contain at least {MinUppercase} uppercase letter.");
            if (lower < MinLowercase) errors.Add($"Password must contain at least {MinLowercase} lowercase letter.");
            if (digits < MinDigits) errors.Add($"Password must contain at least {MinDigits} digit.");
            if (nonAlnum < MinNonAlphanumeric) errors.Add($"Password must contain at least {MinNonAlphanumeric} special character.");
            if (unique < MinUniqueChars) errors.Add($"Password must contain at least {MinUniqueChars} distinct characters.");
            if (maxRun > MaxRepeatRun) errors.Add($"Password must not contain runs of more than {MaxRepeatRun} identical characters.");

            // Avoid passwords derived from account name
            if (!string.IsNullOrWhiteSpace(accountName))
            {
                var normName = NormalizeForCompare(accountName);
                if (!string.IsNullOrEmpty(normName) && IsDerived(password, normName))
                    errors.Add("Password must not be based on the account name.");
            }

            // Avoid passwords derived from email local part
            if (!string.IsNullOrWhiteSpace(email))
            {
                var local = email!.Split('@')[0];
                var normLocal = NormalizeForCompare(local);
                if (!string.IsNullOrEmpty(normLocal) && IsDerived(password, normLocal))
                    errors.Add("Password must not be based on the email address.");
            }

            return errors;
        }

        // --- Helper functions ---

        /// <summary>
        /// Normalizes the string for comparison by removing non-alphanumeric characters
        /// and converting to lowercase.
        /// </summary>
        private static string NormalizeForCompare(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch))
                    sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Checks if the password contains the core string (or its reverse),
        /// which would indicate it is derived from it.
        /// </summary>
        private static bool IsDerived(string password, string core)
        {
            if (core.Length < 3) return false; // Ignore very short cores
            var p = NormalizeForCompare(password);
            if (p.Contains(core)) return true;

            // Reverse core check
            var rev = new string(core.Reverse().ToArray());
            if (p.Contains(rev)) return true;

            return false;
        }
    }
}
