using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IAM.Identities.Service.Implementations.Helpers
{
    public class TokenService
    {
        private const string _secretKey = "EzEgyTitkosKulcsLegalabb32Karakter";
        private const string _issuer = "docratis";
        private const string _audience = "MyAppUsers";
        private const int AccessTokenValidaionTimeInMinutes = 24 * 60;
        private const int RefresokenValidaionTimeInDays = 7;
        private const int EmailConfirmationTokenValidityInMinutes = 24 * 60;

        /// <summary>
        /// Access token generálása rövid lejárati idővel (pl. 15 perc)
        /// </summary>
        public (string accessToken, DateTime expiresAt) GenerateAccessToken(string userId, string userName, string[] roles, List<Claim> additioinalClaims = default, int customTokenValidityInMinutes = default)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim> {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.UniqueName, userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            foreach (var claim in additioinalClaims ?? Enumerable.Empty<Claim>())
                claims.Add(claim);

            var tokenValidityInMinutes = customTokenValidityInMinutes != default
                ? customTokenValidityInMinutes
                : AccessTokenValidaionTimeInMinutes;
            var expiresAt = DateTime.UtcNow.AddMinutes(tokenValidityInMinutes);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        public (string refreshToken, DateTime expiresAt) GenerateRefreshToken(string userId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim> {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var expiresAt = DateTime.UtcNow.AddMinutes(RefresokenValidaionTimeInDays);
            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(RefresokenValidaionTimeInDays),
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        // <summary>
        /// Ellenőrzi a refresh tokent, és visszaadja a felhasználó azonosítóját.
        /// Ha érvénytelen vagy lejárt → null-t ad vissza.
        /// </summary>
        public string ValidateRefreshToken(string refreshToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            try
            {
                var principal = tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                }, out SecurityToken validatedToken);

                // A JWT "sub" claim tartalmazza a userId-t
                var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                return userId;
            }
            catch
            {
                // Ha bármilyen hiba van (lejárt, aláírás rossz, stb.) → érvénytelen token
                return null;
            }
        }

        /// <summary>
        /// Generates a short-lived token for confirming an email address.
        /// </summary>
        public (string token, DateTime expiresAt) GenerateEmailConfirmationToken(string accountId, string authId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim> {
                new Claim("accountId", accountId),
                new Claim("authId", authId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var expiresAt = DateTime.UtcNow.AddMinutes(EmailConfirmationTokenValidityInMinutes);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        /// <summary>
        /// Validates an email confirmation token and returns the accountId/authId if valid.
        /// Returns null if invalid or expired.
        /// </summary>
        public (string AccountId, string AuthId)? ValidateEmailConfirmationToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                }, out SecurityToken validatedToken);

                var accountId = principal.FindFirst("accountId")?.Value;
                var authId = principal.FindFirst("authId")?.Value;

                if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(authId))
                    return null;

                return (accountId, authId);
            }
            catch
            {
                return null;
            }
        }
    }
}
