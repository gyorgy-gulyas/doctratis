using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using ServiceKit.Net;

namespace IAM.Identities.Context.Implementations
{
    public class CertificateAuthorityACL_HasiCorp : ICertificateAuthorityACL
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _mount;
        private readonly string _defaultRole;

        public CertificateAuthorityACL_HasiCorp(HttpClient http, IConfiguration configuration)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));

            _baseUrl = configuration["Vault:BaseUrl"]
                ?? throw new InvalidOperationException("Configuration missing: Vault:BaseUrl");
            _mount = configuration["Vault:Mount"]
                ?? throw new InvalidOperationException("Configuration missing: Vault:Mount (e.g. 'pki' or 'pki_int')");
            _defaultRole = configuration["Vault:DefaultRole"]
                ?? throw new InvalidOperationException("Configuration missing: Vault:DefaultRole");

            if (_http.BaseAddress is null)
                _http.BaseAddress = new Uri(_baseUrl.TrimEnd('/') + "/");

            var token = configuration["Vault:Token"];
            if (!string.IsNullOrWhiteSpace(token))
                _http.DefaultRequestHeaders.Add("X-Vault-Token", token);

            var ns = configuration["Vault:Namespace"];
            if (!string.IsNullOrWhiteSpace(ns))
                _http.DefaultRequestHeaders.Add("X-Vault-Namespace", ns);
        }

        // --- DTO-k ---
        private sealed record SignRequest(string csr, string format);      // format: "der" | "pem"
        private sealed record SignResponse(SignData data);
        private sealed record SignData(string certificate, string issuing_ca, string[] ca_chain);
        private sealed record RevokeRequest(string serial_number);
        private sealed record RevokeResponse(bool? success, string warnings);

        Task<Response<byte[]>> ICertificateAuthorityACL.signCsr(CallingContext ctx, string csrPem, string profile)
        {
            if (string.IsNullOrWhiteSpace(csrPem))
                return new Response<byte[]>(new Error { Status = Statuses.BadRequest, MessageText = "CSR PEM must be provided." }).AsTask();

            var role = string.IsNullOrWhiteSpace(profile) ? _defaultRole : profile;
            return SignInternalAsync(csrPem, role);
        }

        private async Task<Response<byte[]>> SignInternalAsync(string csrPem, string role)
        {
            // Kérjük DER formátumban; ha a Vault mégis PEM-et ad, fallback-kel konvertálunk.
            var path = $"v1/{_mount.TrimEnd('/')}/sign/{Uri.EscapeDataString(role)}";
            using var req = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new SignRequest(csrPem, "der"))
            };
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage resp;
            try
            {
                resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex)
            {
                return new Response<byte[]>(new Error { Status = Statuses.Timeout, MessageText = "Vault sign request timed out.", AdditionalInformation = ex.Message });
            }
            catch (Exception ex)
            {
                return new Response<byte[]>(new Error { Status = Statuses.InternalError, MessageText = "Vault sign request failed.", AdditionalInformation = ex.Message });
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = await SafeReadString(resp).ConfigureAwait(false);
                return new Response<byte[]>(new Error
                {
                    Status = MapStatus(resp.StatusCode),
                    MessageText = "Vault sign returned non-success status.",
                    AdditionalInformation = body
                });
            }

            try
            {
                var json = await resp.Content.ReadFromJsonAsync<SignResponse>().ConfigureAwait(false);
                if (json?.data?.certificate is null)
                    return new Response<byte[]>(new Error { Status = Statuses.InternalError, MessageText = "Vault response missing data.certificate." });

                // Ha DER-t kaptunk (base64-elve), dekódoljuk; ha PEM-et kaptunk, visszaadjuk a PEM szöveget bytes-ként.
                var certStr = json.data.certificate.Trim();
                if (certStr.Contains("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal))
                {
                    // PEM → a felsőbb réteg parsolhatja; vagy konvertáljunk DER-re:
                    try
                    {
                        var der = PemToDer(certStr);
                        return Response.Success(der);
                    }
                    catch
                    {
                        // ha nem sikerül, adjuk vissza PEM-ként (bytes)
                        return Response.Success(Encoding.UTF8.GetBytes(certStr));
                    }
                }
                else
                {
                    // feltételezzük: base64 DER
                    try
                    {
                        var der = Convert.FromBase64String(certStr);
                        return Response.Success(der);
                    }
                    catch (FormatException ex)
                    {
                        return new Response<byte[]>(new Error { Status = Statuses.InternalError, MessageText = "Invalid certificate format from Vault.", AdditionalInformation = ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                return new Response<byte[]>(new Error { Status = Statuses.InternalError, MessageText = "Failed to parse Vault sign response.", AdditionalInformation = ex.Message });
            }
        }

        Task<Response<bool>> ICertificateAuthorityACL.revoke(CallingContext ctx, string serialNumber, string reason)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                return new Response<bool>(new Error { Status = Statuses.BadRequest, MessageText = "serialNumber is required." }).AsTask();

            return RevokeInternalAsync(serialNumber);
        }

        private async Task<Response<bool>> RevokeInternalAsync(string serialNumber)
        {
            var path = $"v1/{_mount.TrimEnd('/')}/revoke";
            using var req = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new RevokeRequest(serial_number: serialNumber))
            };
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage resp;
            try
            {
                resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex)
            {
                return new Response<bool>(new Error { Status = Statuses.Timeout, MessageText = "Vault revoke request timed out.", AdditionalInformation = ex.Message });
            }
            catch (Exception ex)
            {
                return new Response<bool>(new Error { Status = Statuses.InternalError, MessageText = "Vault revoke request failed.", AdditionalInformation = ex.Message });
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = await SafeReadString(resp).ConfigureAwait(false);
                return new Response<bool>(new Error
                {
                    Status = MapStatus(resp.StatusCode),
                    MessageText = "Vault revoke returned non-success status.",
                    AdditionalInformation = body
                });
            }

            try
            {
                // Vault jellemzően 200-at ad, a body tartalmazhat warningot; ha nincs JSON, tekintsük sikeresnek.
                if (resp.Content.Headers.ContentType?.MediaType?.Equals("application/json", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var json = await resp.Content.ReadFromJsonAsync<RevokeResponse>().ConfigureAwait(false);
                    var ok = json?.success ?? true;
                    return Response.Success(ok);
                }
                return Response.Success(true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(new Error { Status = Statuses.InternalError, MessageText = "Failed to parse Vault revoke response.", AdditionalInformation = ex.Message });
            }
        }

        // --- helpers ---

        private static async Task<string> SafeReadString(HttpResponseMessage resp)
        {
            try { return await resp.Content.ReadAsStringAsync().ConfigureAwait(false); }
            catch { return null; }
        }

        private static Statuses MapStatus(System.Net.HttpStatusCode code) =>
            (int)code switch
            {
                400 => Statuses.BadRequest,
                401 => Statuses.Unauthorized,
                403 => Statuses.Unauthorized,
                404 => Statuses.NotFound,
                408 => Statuses.Timeout,
                409 => Statuses.InternalError,
                429 => Statuses.InternalError,
                >= 500 and < 600 => Statuses.InternalError,
                _ => Statuses.InternalError
            };

        private static byte[] PemToDer(string pem)
        {
            const string begin = "-----BEGIN CERTIFICATE-----";
            const string end = "-----END CERTIFICATE-----";
            var i = pem.IndexOf(begin, StringComparison.Ordinal);
            if (i < 0) throw new FormatException("Missing PEM begin.");
            i += begin.Length;
            var j = pem.IndexOf(end, i, StringComparison.Ordinal);
            if (j < 0) throw new FormatException("Missing PEM end.");
            var body = pem.Substring(i, j - i);
            var b64 = new string(body.Where(c => !char.IsWhiteSpace(c)).ToArray());
            return Convert.FromBase64String(b64);
        }
    }
}
