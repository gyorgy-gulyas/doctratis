using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ServiceKit.Net;

namespace IAM.Identities.Service.Implementations.Helpers
{
    /// <summary>
    /// Provides helper methods for interacting with a Certificate Authority ACL,
    /// including signing CSRs, revoking certificates, and parsing X.509 certificates.
    /// </summary>
    public class CertificateAgent
    {
        private readonly ICertificateAuthorityACL _acl;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAgent"/> class.
        /// </summary>
        /// <param name="acl">The Certificate Authority ACL interface used to perform certificate operations.</param>
        public CertificateAgent(ICertificateAuthorityACL acl)
        {
            _acl = acl;
        }

        /// <summary>
        /// Signs a CSR using the configured Certificate Authority and parses the resulting certificate.
        /// </summary>
        /// <param name="ctx">The calling context for request tracing.</param>
        /// <param name="csrPem">The PEM-encoded Certificate Signing Request (CSR).</param>
        /// <param name="profile">The certificate issuance profile (policy) to use.</param>
        /// <returns>A response containing the parsed certificate details if successful.</returns>
        public async Task<Response<ParsedCert>> SignCsrAndParseAsync(CallingContext ctx, string csrPem, string profile)
        {
            var sign = await _acl.signCsr(ctx, csrPem, profile).ConfigureAwait(false);
            if (sign.IsFailed())
                return new(sign.Error);

            return ParseCertificateBytes(sign.Value);
        }

        /// <summary>
        /// Revokes a certificate in the Certificate Authority by its serial number.
        /// </summary>
        /// <param name="ctx">The calling context for request tracing.</param>
        /// <param name="serialNumber">The serial number of the certificate to revoke.</param>
        /// <param name="reason">The reason for revocation.</param>
        /// <returns>A response indicating whether the revocation succeeded.</returns>
        public async Task<Response> RevokeBySerialAsync(CallingContext ctx, string serialNumber, string reason)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                return Response.Failure(new Error { Status = Statuses.BadRequest, MessageText = "Missing certificate serialNumber." });

            var ca = await _acl.revoke(ctx, serialNumber, reason).ConfigureAwait(false);
            if (ca.IsFailed())
                return new(ca.Error);

            if (!ca.Value)
                return Response.Failure(new Error { Status = Statuses.InternalError, MessageText = "CA failed to revoke certificate." });

            return Response.Success();
        }

        /// <summary>
        /// Parses a raw certificate byte array (PEM or DER) into a structured representation
        /// and calculates certificate hashes.
        /// </summary>
        /// <param name="bytes">The certificate in DER or PEM format.</param>
        /// <returns>A response containing a <see cref="ParsedCert"/> with details about the certificate.</returns>
        public Response<ParsedCert> ParseCertificateBytes(byte[] bytes)
        {
            if (bytes is null || bytes.Length == 0)
                return new Response<ParsedCert>(new Error { Status = Statuses.BadRequest, MessageText = "Empty certificate payload." });

            try
            {
                X509Certificate2 cert;

                if (LooksLikePem(bytes))
                {
                    var pem = Encoding.UTF8.GetString(bytes);
                    cert = X509Certificate2.CreateFromPem(pem);
                    // Materialize as DER-only instance to ensure RawData is populated cleanly
                    cert = new X509Certificate2(cert.Export(X509ContentType.Cert));
                }
                else
                {
                    cert = new X509Certificate2(bytes);
                }

                var parsed = BuildParsed(cert);
                return Response.Success(parsed);
            }
            catch (Exception ex)
            {
                return new Response<ParsedCert>(new Error
                {
                    Status = Statuses.InternalError,
                    MessageText = "Failed to parse certificate.",
                    AdditionalInformation = ex.Message
                });
            }
        }

        /// <summary>
        /// Builds a <see cref="ParsedCert"/> structure from an <see cref="X509Certificate2"/> instance.
        /// </summary>
        /// <param name="cert">The certificate to parse.</param>
        /// <returns>A <see cref="ParsedCert"/> containing serial, issuer, subject, validity, and hash info.</returns>
        private static ParsedCert BuildParsed(X509Certificate2 cert)
        {
            var thumbSha256 = Convert.ToHexString(SHA256.HashData(cert.RawData));
            var spkiSha256 = Convert.ToHexString(SHA256.HashData(cert.PublicKey.EncodedKeyValue.RawData));

            return new ParsedCert
            {
                SerialNumber = cert.SerialNumber,
                Issuer = cert.Issuer,
                Subject = cert.Subject,
                NotBeforeUtc = cert.NotBefore.ToUniversalTime(),
                NotAfterUtc = cert.NotAfter.ToUniversalTime(),
                ThumbprintSha256 = thumbSha256,
                SpkiSha256 = spkiSha256,
                DerBytes = cert.Export(X509ContentType.Cert)
            };
        }

        /// <summary>
        /// Detects whether the input bytes look like a PEM-encoded certificate.
        /// </summary>
        /// <param name="data">The raw input data.</param>
        /// <returns><c>true</c> if the data appears to be PEM; otherwise, <c>false</c>.</returns>
        private static bool LooksLikePem(byte[] data)
        {
            if (data.Length < 30) return false;

            int asciiCount = 0;
            int check = Math.Min(data.Length, 256);
            for (int i = 0; i < check; i++)
            {
                if (data[i] >= 0x20 && data[i] <= 0x7E) asciiCount++;
            }
            if (asciiCount > check * 0.8)
            {
                var head = Encoding.UTF8.GetString(data, 0, Math.Min(data.Length, 256));
                if (head.Contains("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Represents a parsed X.509 certificate with common fields and calculated hashes.
    /// </summary>
    public sealed class ParsedCert
    {
        /// <summary>
        /// Gets the serial number of the certificate.
        /// </summary>
        public string SerialNumber { get; init; }

        /// <summary>
        /// Gets the issuer distinguished name (DN).
        /// </summary>
        public string Issuer { get; init; }

        /// <summary>
        /// Gets the subject distinguished name (DN).
        /// </summary>
        public string Subject { get; init; }

        /// <summary>
        /// Gets the UTC validity start date.
        /// </summary>
        public DateTime NotBeforeUtc { get; init; }

        /// <summary>
        /// Gets the UTC validity end date.
        /// </summary>
        public DateTime NotAfterUtc { get; init; }

        /// <summary>
        /// Gets the SHA-256 thumbprint (hash of the DER-encoded certificate).
        /// </summary>
        public string ThumbprintSha256 { get; init; }

        /// <summary>
        /// Gets the SHA-256 hash of the certificate’s SubjectPublicKeyInfo (SPKI).
        /// </summary>
        public string SpkiSha256 { get; init; }

        /// <summary>
        /// Gets the DER-encoded certificate bytes.
        /// </summary>
        public byte[] DerBytes { get; init; }
    }
}
