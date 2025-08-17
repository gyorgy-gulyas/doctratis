using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using ServiceKit.Net;


namespace IAM.Identities.Context.Implementations
{
    public class CertificateAuthorityACL_BouncyCastle : ICertificateAuthorityACL
    {
        // CA private key és cert (ideális esetben configból vagy HSM-ből jön)
        private readonly AsymmetricKeyParameter _caPrivateKey;
        private readonly X509Certificate _caCertificate;

        // Egyszerű CRL lista memóriában
        private readonly HashSet<string> _revokedSerialNumbers = new();

        public CertificateAuthorityACL_BouncyCastle( IConfiguration configuration)
        {
            // Betöltés fájlból (vagy generálás teszt célra)
            (_caPrivateKey, _caCertificate) = LoadCaFromPem("ca-cert.pem", "ca-key.pem");
        }

        public Task<Response<byte[]>> signCsr(CallingContext ctx, string csrPem, string profile)
        {
            try
            {
                // CSR parse
                using var reader = new StringReader(csrPem);
                var pemReader = new PemReader(reader);
                var csr = (Pkcs10CertificationRequest)pemReader.ReadObject();
                if (!csr.Verify())
                    return new Response<byte[]>(new Error { Status = Statuses.BadRequest, MessageText = "Invalid CSR" }).AsTask();

                var cert = SignCsr(csr);

                var certBytes = cert.GetEncoded();
                return Response.Success(certBytes).AsTask();
            }
            catch (Exception ex)
            {
                return new Response<byte[]>(new Error { Status = Statuses.InternalError, MessageText = $"Signing error: {ex.Message}" }).AsTask();
            }
        }

        public Task<Response<bool>> revoke(CallingContext ctx, string serialNumber, string reason)
        {
            _revokedSerialNumbers.Add(serialNumber);
            return Response.Success(true).AsTask();
        }

        // CSR -> X509Certificate
        private X509Certificate SignCsr(Pkcs10CertificationRequest csr)
        {
            var pubKey = PublicKeyFactory.CreateKey(csr.GetCertificationRequestInfo().SubjectPublicKeyInfo);
            var signatureFactory = new Asn1SignatureFactory("SHA256WITHRSA", _caPrivateKey);

            var certGen = new X509V3CertificateGenerator();
            certGen.SetSerialNumber(BigInteger.ProbablePrime(120, new SecureRandom()));
            certGen.SetIssuerDN(_caCertificate.SubjectDN);
            certGen.SetSubjectDN(csr.GetCertificationRequestInfo().Subject);
            certGen.SetNotBefore(DateTime.UtcNow);
            certGen.SetNotAfter(DateTime.UtcNow.AddYears(1));
            certGen.SetPublicKey(pubKey);

            certGen.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(false));
            certGen.AddExtension(X509Extensions.AuthorityKeyIdentifier, false,new AuthorityKeyIdentifierStructure(_caCertificate));
            certGen.AddExtension(X509Extensions.SubjectKeyIdentifier, false,new SubjectKeyIdentifierStructure(pubKey));

            return certGen.Generate(signatureFactory);
        }

        // CA betöltése PEM fájlokból
        private (AsymmetricKeyParameter, X509Certificate) LoadCaFromPem(string certPath, string keyPath)
        {
            using var certReader = new StreamReader(certPath);
            using var keyReader = new StreamReader(keyPath);

            var pemCert = (X509Certificate)new PemReader(certReader).ReadObject();
            var pemKey = (AsymmetricKeyParameter)new PemReader(keyReader).ReadObject();

            return (pemKey, pemCert);
        }
    }
}
