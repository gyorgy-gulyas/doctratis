using System;

namespace Core.Identities.Service.Implementations.Helpers
{
    public class KAUAuthenticator
    {
        private readonly string _authEndpoint = "https://idp.ka.gov.hu/authorize";
        private readonly string _tokenEndpoint = "https://idp.ka.gov.hu/token";
        private readonly string _kauClientId;
        private readonly string _kauClientSecret;
        private readonly string _secretKey;
        private readonly HttpClient _httpClient;

        public KAUAuthenticator(HttpClient http, IConfiguration config)
        {
            _kauClientId = config["KAU:ClientId"];
            _kauClientSecret = config["KAU:ClientSecret"];
            _secretKey = config["KAU:StateSecret"];
            _httpClient = http;
        }

        public string GenerateUniqueState(string returnUrl)
        {
            var payload = new StatePayload()
            {
                ReturnUrl = returnUrl,
                Nonce = Guid.NewGuid().ToString(),
            };

            var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
            var payloadBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payloadJson));

            // HMAC-SHA256 aláírás
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(_secretKey));
            var signature = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payloadBase64));
            var signatureBase64 = Convert.ToBase64String(signature);

            // payload + signature (pl.: payload.signature)
            return $"{payloadBase64}.{signatureBase64}";
        }

        public bool ValidateState(string state, out string returnUrl)
        {
            returnUrl = null;

            var parts = state.Split('.');
            if (parts.Length != 2)
                return false;

            var payloadBase64 = parts[0];
            var signatureBase64 = parts[1];

            // újra számoljuk a HMAC-et
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(_secretKey));
            var expectedSignature = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payloadBase64));
            var expectedSignatureBase64 = Convert.ToBase64String(expectedSignature);

            if (expectedSignatureBase64 != signatureBase64)
                return false; // nem tőlünk származik

            // payload deszerializálása
            var payloadJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payloadBase64));
            var payload = System.Text.Json.JsonSerializer.Deserialize<StatePayload>(payloadJson);

            returnUrl = payload.ReturnUrl;

            return true;
        }

        public string GetLoginUrl(string state, string backendCallbackUrl)
        {
            return $"{_authEndpoint}?" +
                   $"client_id={_kauClientId}&" +
                   $"response_type=code&" +
                   $"scope=openid profile email&" +
                   $"redirect_uri={Uri.EscapeDataString(backendCallbackUrl)}&" +
                   $"state={state}";
        }

        public async Task<KauTokenResponse?> ExchangeCodeForTokenAsync(string code, string backendCallbackUrl)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = backendCallbackUrl,
                ["client_id"] = _kauClientId,
                ["client_secret"] = _kauClientSecret
            });

            var response = await _httpClient.PostAsync(_tokenEndpoint, content);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<KauTokenResponse>(json);
        }

        public class KauTokenResponse
        {
            public string access_token { get; set; }
            public string id_token { get; set; }
            public string refresh_token { get; set; }
            public int expires_in { get; set; }
            public string token_type { get; set; }
        }

        public class KauUserInfo
        {
            public string UserId { get; set; }
            public string? Name { get; set; }
            public string? Email { get; set; }
        }

        internal class StatePayload
        {
            public string ReturnUrl { get; set; }
            public string Nonce { get; set; }
        }
    }
}
