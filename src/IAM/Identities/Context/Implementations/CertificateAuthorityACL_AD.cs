using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using ServiceKit.Net;

namespace IAM.Identities.Context.Implementations
{
    public class CertificateAuthorityACL_AD : ICertificateAuthorityACL
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _issuePath;
        private readonly string _revokePath;
        private readonly string _defaultTemplate;

        public CertificateAuthorityACL_AD(HttpClient http, IConfiguration configuration)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _baseUrl = configuration["Adcs:BaseUrl"]
                ?? throw new InvalidOperationException("Configuration missing: Adcs:BaseUrl");
            _issuePath = configuration["Adcs:IssuePath"] ?? "/api/certificates/issue";
            _revokePath = configuration["Adcs:RevokePath"] ?? "/api/certificates/revoke";
            _defaultTemplate = configuration["Adcs:DefaultTemplate"];

            // Opcionális: auth header beállítás konfigurációból
            var scheme = configuration["Adcs:Auth:Scheme"]; // "Bearer" / "Basic" / "Negotiate"
            var token = configuration["Adcs:Auth:Token"];
            if (!string.IsNullOrWhiteSpace(scheme) && !string.IsNullOrWhiteSpace(token))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, token);
            }

            if (_http.BaseAddress is null)
                _http.BaseAddress = new Uri(_baseUrl.TrimEnd('/') + "/");
        }

        // --- DTO-k a REST hívásokhoz ---
        private sealed record IssueRequest(string csrPem, string template);
        private sealed record IssueResponse(string pem, string derBase64, string certificate); // certificate lehet PEM vagy Base64
        private sealed record RevokeRequest(string serialNumber, string reason);
        private sealed record RevokeResponse(bool success);

        // --- ICertificateAuthorityACL implementáció ---

        Task<Response<byte[]>> ICertificateAuthorityACL.signCsr(CallingContext ctx, string csrPem, string profile)
        {
            if (string.IsNullOrWhiteSpace(csrPem))
                return new Response<byte[]>(new Error { Status = Statuses.BadRequest, MessageText = "CSR PEM must be provided." }).AsTask();

            var template = string.IsNullOrWhiteSpace(profile) ? _defaultTemplate : profile;
            if (string.IsNullOrWhiteSpace(template))
                return new Response<byte[]>(new Error { Status = Statuses.BadRequest, MessageText = "Certificate template (profile) is required." }).AsTask();

            return SignInternalAsync(csrPem, template);
        }

        private async Task<Response<byte[]>> SignInternalAsync(string csrPem, string template)
        {
            // Kérjük JSON-ként; elfogadunk többféle választ
            using var req = new HttpRequestMessage(HttpMethod.Post, _issuePath)
            {
                Content = JsonContent.Create(new IssueRequest(csrPem, template))
            };
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pkix-cert"));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

            HttpResponseMessage resp;
            try
            {
                resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex)
            {
                return new Response<byte[]>(new Error { Status = Statuses.Timeout, MessageText = "AD CS REST sign request timed out.", AdditionalInformation = ex.Message });
            }
            catch (Exception ex)
            {
                return new Response<byte[]>(new Error { Status = Statuses.InternalError, MessageText = "AD CS REST sign request failed.", AdditionalInformation = ex.Message });
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = await SafeReadString(resp).ConfigureAwait(false);
                return new Response<byte[]>(new Error
                {
                    Status = MapStatus(resp.StatusCode),
                    MessageText = "AD CS REST sign returned non-success status.",
                    AdditionalInformation = body
                });
            }

            // Tartalom feldolgozása
            var contentType = resp.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();

            try
            {
                switch (contentType)
                {
                    case "application/json":
                        {
                            var json = await resp.Content.ReadFromJsonAsync<IssueResponse>().ConfigureAwait(false);
                            if (json is null)
                                return new Response<byte[]>(new Error { Status = Statuses.InternalError, MessageText = "Empty JSON from AD CS REST." });

                            // 1) explicit PEM mező
                            if (!string.IsNullOrWhiteSpace(json.pem))
                                return Response.Success(Encoding.UTF8.GetBytes(json.pem));

                            // 2) explicit derBase64 mező
                            if (!string.IsNullOrWhiteSpace(json.derBase64))
                            {
                                try
                                {
                                    var der = Convert.FromBase64String(json.derBase64);
                                    return Response.Success(der);
                                }
                                catch
                                {
                                    return new Response<byte[]>(new Error { Status = Statuses.InternalError, MessageText = "Invalid derBase64 in AD CS REST response." });
                                }
                            }

                            // 3) "certificate" mező (lehet PEM vagy Base64 DER)
                            if (!string.IsNullOrWhiteSpace(json.certificate))
                            {
                                // Ha PEM header van, nyers bytes-ként adjuk vissza a PEM-et (felső réteg parsolja)
                                if (json.certificate.Contains("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal))
                                    return Response.Success(Encoding.UTF8.GetBytes(json.certificate));

                                // egyébként próbáljuk Base64-nek
                                try
                                {
                                    var der = Convert.FromBase64String(json.certificate);
                                    return Response.Success(der);
                                }
                                catch
                                {
                                    return new Response<byte[]>(new Error { Status = Statuses.InternalError, MessageText = "Unrecognized certificate format in JSON response." });
                                }
                            }

                            return new Response<byte[]>(new Error { Status = Statuses.InternalError, MessageText = "No certificate payload in AD CS REST response." });
                        }

                    case "application/pkix-cert":
                    case "application/octet-stream":
                        {
                            var bytes = await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                            if (bytes is null || bytes.Length == 0)
                                return new Response<byte[]>(new Error { Status = Statuses.InternalError, MessageText = "Empty certificate payload from AD CS REST." });
                            return Response.Success(bytes);
                        }

                    case "text/plain":
                    default:
                        {
                            var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (string.IsNullOrWhiteSpace(text))
                                return new Response<byte[]>(new Error { Status = Statuses.InternalError, MessageText = "Empty certificate payload (text) from AD CS REST." });

                            // Ha PEM-nek tűnik, adjuk vissza mint PEM szöveg bytes
                            if (text.Contains("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal))
                                return Response.Success(Encoding.UTF8.GetBytes(text));

                            // Ellenkező esetben próbáljuk Base64-nek
                            try
                            {
                                var der = Convert.FromBase64String(text.Trim());
                                return Response.Success(der);
                            }
                            catch
                            {
                                return new Response<byte[]>(new Error { Status = Statuses.InternalError, MessageText = "Unrecognized text certificate format from AD CS REST." });
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                return new Response<byte[]>(new Error { Status = Statuses.InternalError, MessageText = "Failed to process AD CS REST sign response.", AdditionalInformation = ex.Message });
            }
        }

        Task<Response<bool>> ICertificateAuthorityACL.revoke(CallingContext ctx, string serialNumber, string reason)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                return new Response<bool>(new Error { Status = Statuses.BadRequest, MessageText = "serialNumber is required." }).AsTask();
            if (string.IsNullOrWhiteSpace(reason))
                return new Response<bool>(new Error { Status = Statuses.BadRequest, MessageText = "reason is required." }).AsTask();

            return RevokeInternalAsync(serialNumber, reason);
        }

        private async Task<Response<bool>> RevokeInternalAsync(string serialNumber, string reason)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, _revokePath)
            {
                Content = JsonContent.Create(new RevokeRequest(serialNumber, reason))
            };
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

            HttpResponseMessage resp;
            try
            {
                resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex)
            {
                return new Response<bool>(new Error { Status = Statuses.Timeout, MessageText = "AD CS REST revoke request timed out.", AdditionalInformation = ex.Message });
            }
            catch (Exception ex)
            {
                return new Response<bool>(new Error { Status = Statuses.InternalError, MessageText = "AD CS REST revoke request failed.", AdditionalInformation = ex.Message });
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = await SafeReadString(resp).ConfigureAwait(false);
                return new Response<bool>(new Error
                {
                    Status = MapStatus(resp.StatusCode),
                    MessageText = "AD CS REST revoke returned non-success status.",
                    AdditionalInformation = body
                });
            }

            var contentType = resp.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();

            try
            {
                if (contentType == "application/json")
                {
                    var json = await resp.Content.ReadFromJsonAsync<RevokeResponse>().ConfigureAwait(false);
                    var ok = json?.success ?? true; // ha nincs mező, 200 → true
                    return Response.Success(ok);
                }

                // text/plain vagy üres törzs → tekintsük sikeresnek 200 esetén
                return Response.Success(true);
            }
            catch (Exception ex)
            {
                return new Response<bool>(new Error { Status = Statuses.InternalError, MessageText = "Failed to process AD CS REST revoke response.", AdditionalInformation = ex.Message });
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
    }
}
