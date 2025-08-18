using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using ServiceKit.Net;
using System.Text;

namespace IAM.Identities.Tests.Mock
{
    internal class Mock_CertificateAuthorityACL : ICertificateAuthorityACL
    {
        private readonly HashSet<string> _revokedSerials = new();
        private readonly AsymmetricKeyParameter _caPrivateKey;
        private readonly X509Certificate _caCert;

        public Mock_CertificateAuthorityACL()
        {
            // 1) CA kulcspár generálás
            var keyGen = new RsaKeyPairGenerator();
            keyGen.Init(new KeyGenerationParameters(new SecureRandom(), 2048));
            var keyPair = keyGen.GenerateKeyPair();

            // 2) Self-signed CA tanúsítvány generálás
            var certGen = new X509V3CertificateGenerator();
            var caSubject = new X509Name("CN=MockTestCA");
            var serial = BigIntegers.CreateRandomInRange(
                Org.BouncyCastle.Math.BigInteger.One,
                Org.BouncyCastle.Math.BigInteger.ValueOf(long.MaxValue),
                new SecureRandom());

            certGen.SetSerialNumber(serial);
            certGen.SetSubjectDN(caSubject);
            certGen.SetIssuerDN(caSubject);
            certGen.SetNotBefore(DateTime.UtcNow.AddDays(-1));
            certGen.SetNotAfter(DateTime.UtcNow.AddYears(5));
            certGen.SetPublicKey(keyPair.Public);

            // CA bit
            certGen.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(true));

            _caPrivateKey = keyPair.Private;
            _caCert = certGen.Generate(new Asn1SignatureFactory("SHA256WithRSA", keyPair.Private, new SecureRandom()));
        }

        public Task<Response<byte[]>> signCsr(CallingContext ctx, string csrPem, string profile)
        {
            Console.WriteLine($"[MOCK] signCsr called with profile={profile}");

            // Parse CSR
            Pkcs10CertificationRequest csr;
            try
            {
                csr = new PemReader(new StringReader(csrPem)).ReadObject() as Pkcs10CertificationRequest;
                if (csr == null)
                    return Response<byte[]>.Failure(new Error() { Status = Statuses.BadRequest, MessageText= "Invalid CSR" }).AsTask();
                    
            }
            catch (Exception ex)
            {
                return Response<byte[]>.Failure(new Error() { Status = Statuses.InternalError, MessageText = $"CSR parse failed: {ex.Message}" }).AsTask();
            }

            // Új cert generálása a CSR alapján
            var certGen = new X509V3CertificateGenerator();
            var serial = BigIntegers.CreateRandomInRange(
                Org.BouncyCastle.Math.BigInteger.One,
                Org.BouncyCastle.Math.BigInteger.ValueOf(long.MaxValue),
                new SecureRandom());

            certGen.SetSerialNumber(serial);
            certGen.SetIssuerDN(_caCert.SubjectDN);
            certGen.SetSubjectDN(csr.GetCertificationRequestInfo().Subject);
            certGen.SetNotBefore(DateTime.UtcNow.AddMinutes(-1));
            certGen.SetNotAfter(DateTime.UtcNow.AddYears(1));
            certGen.SetPublicKey(csr.GetPublicKey());

            var issued = certGen.Generate(new Asn1SignatureFactory("SHA256WithRSA", _caPrivateKey, new SecureRandom()));

            // PEM formátumban visszaadjuk
            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            {
                var pemWriter = new PemWriter(sw);
                pemWriter.WriteObject(issued);
            }

            var certBytes = Encoding.UTF8.GetBytes(sb.ToString());
            return Response.Success(certBytes).AsTask();
        }

        public Task<Response<bool>> revoke(CallingContext ctx, string serialNumber, string reason)
        {
            Console.WriteLine($"[MOCK] revoke called with serial={serialNumber}, reason={reason}");

            _revokedSerials.Add(serialNumber);
            return Response.Success(true).AsTask();
        }
    }
}
