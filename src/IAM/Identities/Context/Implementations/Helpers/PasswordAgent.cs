using ServiceKit.Net;
using System.Security.Cryptography;

namespace IAM.Identities.Service.Implementations.Helpers
{
    public class PasswordAgent
    {
        // === Hashing parameters ===
        private readonly int Hash_Iterations = 10_000; // PBKDF2 iterations
        private readonly int Hash_KeySize = 32;        // 256-bit derived key length
        private readonly int Salt_Length = 16;         // 128-bit random salt size
        // === Password history ===
        private readonly int History_MaxCount = 12;    // Store last N password hashes to prevent reuse
        // === Expiration ===
        private readonly int ExpirationDays = 30;      // Default password expiration in days

        // === Password complexity rules ===
        private readonly int MinLength = 12;
        private readonly int MaxLength = 128;
        private readonly int MinUppercase = 1;       // Minimum uppercase letters required
        private readonly int MinLowercase = 1;       // Minimum lowercase letters required
        private readonly int MinDigits = 1;          // Minimum digits required
        private readonly int MinNonAlphanumeric = 1; // Minimum special characters required
        private readonly int MinUniqueChars = 5;     // Require at least N distinct characters
        private readonly int MaxRepeatRun = 3;       // Same character can repeat max N times in a row


        public int GetExpirationDays() => ExpirationDays;

        public string CreateLifetimeSalt()
        {
            var salt = RandomNumberGenerator.GetBytes(Salt_Length);
            return Convert.ToBase64String(salt);
        }

        public Response<string> GeneratePasswordHash(string password, string lifetimeSalt)
        {
            byte[] saltBytes;
            try
            {
                saltBytes = Convert.FromBase64String(lifetimeSalt);
            }
            catch (FormatException)
            {
                return new(new Error { Status = Statuses.InternalError, MessageText = "Stored password salt is invalid (Base64 decode failed)." });
            }

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password, 
                saltBytes, 
                Hash_Iterations, 
                HashAlgorithmName.SHA256);
            var passwordHash = pbkdf2.GetBytes(Hash_KeySize);

            return new(Convert.ToBase64String(passwordHash));
        }

        // --- Local helper functions ---
        public bool IsPasswordValid(string password, string lifetimeSalt, string currentPasswordHash )
        {
            byte[] saltBytes;
            try
            {
                saltBytes = Convert.FromBase64String(lifetimeSalt);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] hashBytes;
            try
            {
                hashBytes = Convert.FromBase64String(currentPasswordHash);
            }
            catch (FormatException)
            {
                return false;
            }

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                saltBytes,
                Hash_Iterations,
                HashAlgorithmName.SHA256);
            var computedHash = pbkdf2.GetBytes(Hash_KeySize);

            // Constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(computedHash, hashBytes);
        }


        /// <summary>
        /// Validates the given password against all rules.
        /// Returns a list of human-readable error messages.
        /// If the list is empty, the password is valid.
        /// </summary>
        public IReadOnlyList<string> ValidatePasswordRules(string password, string accountName = null, string email = null)
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
                var normName = _NormalizeForCompare(accountName);
                if (!string.IsNullOrEmpty(normName) && _IsDerived(password, normName))
                    errors.Add("Password must not be based on the account name.");
            }

            // Avoid passwords derived from email local part
            if (!string.IsNullOrWhiteSpace(email))
            {
                var local = email!.Split('@')[0];
                var normLocal = _NormalizeForCompare(local);
                if (!string.IsNullOrEmpty(normLocal) && _IsDerived(password, normLocal))
                    errors.Add("Password must not be based on the email address.");
            }

            return errors;
        }

        /// <summary>
        /// Normalizes the string for comparison by removing non-alphanumeric characters
        /// and converting to lowercase.
        /// </summary>
        private static string _NormalizeForCompare(string s)
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
        private static bool _IsDerived(string password, string core)
        {
            if (core.Length < 3) return false; // Ignore very short cores
            var p = _NormalizeForCompare(password);
            if (p.Contains(core)) return true;

            // Reverse core check
            var rev = new string(core.Reverse().ToArray());
            if (p.Contains(rev)) return true;

            return false;
        }

        public Response CheckCurrentPassword( string newPassword, string lifetimeSalt, string currentPasswordHash)
        {
            byte[] saltBytes;
            try
            {
                saltBytes = Convert.FromBase64String(lifetimeSalt);
            }
            catch (FormatException)
            {
                return new(new Error { Status = Statuses.InternalError, MessageText = "Stored password salt is invalid (Base64 decode failed)." });
            }

            using var pbkdf2 = new Rfc2898DeriveBytes(
                newPassword, 
                saltBytes, 
                Hash_Iterations, 
                HashAlgorithmName.SHA256);
            byte[] newHashBytes = pbkdf2.GetBytes(Hash_KeySize);

            // Don't use the same password as your CURRENT password
            try
            {
                var currentHashBytes = Convert.FromBase64String(currentPasswordHash);
                if (CryptographicOperations.FixedTimeEquals(newHashBytes, currentHashBytes))
                {
                    return new(new Error
                    {
                        Status = Statuses.BadRequest,
                        MessageText = "New password must be different from the current password."
                    });
                }
            }
            catch (FormatException)
            {
                return new(new Error { Status = Statuses.InternalError, MessageText = "Stored password hash is invalid (Base64 decode failed)." });
            }

            return Response.Success();
        }


        public Response CheckPasswordHistory(string newPassword, string lifetimeSalt, List<string> passwordHistory)
        {
            byte[] saltBytes;
            try
            {
                saltBytes = Convert.FromBase64String(lifetimeSalt);
            }
            catch (FormatException)
            {
                return new(new Error { Status = Statuses.InternalError, MessageText = "Stored password salt is invalid (Base64 decode failed)." });
            }

            using var pbkdf2 = new Rfc2898DeriveBytes(
                newPassword, 
                saltBytes, 
                Hash_Iterations, 
                HashAlgorithmName.SHA256);
            byte[] newHashBytes = pbkdf2.GetBytes(Hash_KeySize);

            // Do not use any of your previous passwords
            if (passwordHistory != null && passwordHistory.Count > 0)
            {
                foreach (var prevHashB64 in passwordHistory)
                {
                    if (string.IsNullOrWhiteSpace(prevHashB64))
                        continue;

                    byte[] prevHashBytes;
                    try
                    {
                        prevHashBytes = Convert.FromBase64String(prevHashB64);
                    }
                    catch
                    {
                        // Damaged history: you can consider it a mistake; let's skip it here.
                        continue;
                    }

                    if (CryptographicOperations.FixedTimeEquals(newHashBytes, prevHashBytes))
                    {
                        return new(new Error
                        {
                            Status = Statuses.BadRequest,
                            MessageText = "New password must not match any of the previously used passwords."
                        });
                    }
                }
            }

            return Response.Success();
        }

        public void AppendToHistory(List<string> passwordHistory, string currentPasswordHash, string newPasswordHash)
        {
            // History update (current hash in, FIFO limit optional)
            passwordHistory ??= [];
            if (!string.IsNullOrEmpty(currentPasswordHash))
            {
                passwordHistory.Add(currentPasswordHash);

                if (History_MaxCount > 0 && passwordHistory.Count > History_MaxCount)
                {
                    // tartsuk az utolsó N-et
                    var toRemove = passwordHistory.Count - History_MaxCount;
                    passwordHistory.RemoveRange(0, toRemove);
                }
            }
        }
    }

}
