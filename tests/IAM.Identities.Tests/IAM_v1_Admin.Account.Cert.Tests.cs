using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace IAM.Identities.Tests
{
    [TestClass]
    public class IdentityAdminIF_v1_Acccount_Cert_Tests
    {
        private IIdentityAdminIF_v1 Sut =>
            TestMain.ServiceProvider.GetRequiredService<IIdentityAdminIF_v1>();

        [TestInitialize]
        public async Task Setup()
        {
            await TestMain.DeleteAllData();
        }

        // -------------------- Helpers --------------------

        private async Task<IIdentityAdminIF_v1.AccountDTO> CreateUser(string name)
        {
            var res = await Sut.createAccount(TestMain.ctx, name, IIdentityAdminIF_v1.AccountTypes.User);
            Assert.IsTrue(res.IsSuccess(), res.Error?.MessageText);
            return res.Value;
        }

        public static string CreateRsaCsrPem(string subjectCn = "CN=test-user")
        {
            using var rsa = RSA.Create(2048);

            var request = new CertificateRequest(
                subjectCn,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // (opcionális) KeyUsage/ExtendedKeyUsage stb. itt hozzáadható

            byte[] der = request.CreateSigningRequest();

            // PEM-be csomagolás
            var sb = new StringBuilder();
            sb.AppendLine("-----BEGIN CERTIFICATE REQUEST-----");
            sb.AppendLine(Convert.ToBase64String(der, Base64FormattingOptions.InsertLineBreaks));
            sb.AppendLine("-----END CERTIFICATE REQUEST-----");
            return sb.ToString();
        }

        private static IIdentityAdminIF_v1.CsrInputDTO Csr(string pem = default, string profile = null)
        {
            if (pem == default)
                pem = CreateRsaCsrPem();

            return new IIdentityAdminIF_v1.CsrInputDTO { csrPem = pem, profile = profile ?? "" };
        }
       

        // -------------------- createCertificateAuthFromCSR --------------------

        [TestMethod]
        public async Task Certificate_CreateFromCSR_success_minimal()
        {
            var acc = await CreateUser("cert_user_ok");

            var create = await Sut.createCertificateAuthFromCSR(
                TestMain.ctx, acc.id, Csr(profile: null));
            Assert.IsTrue(create.IsSuccess(), create.Error?.MessageText);

            var dto = create.Value;
            Assert.IsFalse(string.IsNullOrEmpty(dto.id));
            Assert.IsFalse(string.IsNullOrEmpty(dto.etag));
            Assert.AreNotEqual(DateTime.MinValue, dto.LastUpdate);
            Assert.IsTrue(dto.isActive);
            Assert.IsFalse(dto.isRevoked);

            // tipikus mezők kitöltve
            Assert.IsFalse(string.IsNullOrEmpty(dto.certificateThumbprint));
            Assert.IsFalse(string.IsNullOrEmpty(dto.serialNumber));
            Assert.IsFalse(string.IsNullOrEmpty(dto.issuer));
            Assert.IsFalse(string.IsNullOrEmpty(dto.subject));
            Assert.AreNotEqual(default, dto.validFrom);
            Assert.AreNotEqual(default, dto.validUntil);
        }

        [TestMethod]
        public async Task Certificate_CreateFromCSR_success_with_profile()
        {
            var acc = await CreateUser("cert_user_profile");

            var create = await Sut.createCertificateAuthFromCSR(
                TestMain.ctx, acc.id, Csr(profile: "client-auth"));
            Assert.IsTrue(create.IsSuccess(), create.Error?.MessageText);
        }

        [TestMethod]
        public async Task Certificate_CreateFromCSR_fail_invalid_csr_or_args()
        {
            var acc = await CreateUser("cert_user_bad");

            // üres CSR
            var empty = await Sut.createCertificateAuthFromCSR(TestMain.ctx, acc.id, Csr(pem: ""));
            Assert.IsTrue(empty.IsFailed(), "Empty CSR should fail.");

            // szemét CSR
            var garbage = await Sut.createCertificateAuthFromCSR(TestMain.ctx, acc.id, Csr(pem: "not a pem"));
            Assert.IsTrue(garbage.IsFailed(), "Invalid CSR should fail.");

            // ismeretlen account
            var badAcc = await Sut.createCertificateAuthFromCSR(TestMain.ctx, "missing-account", Csr());
            Assert.IsTrue(badAcc.IsFailed());
        }

        // -------------------- getCertificateAuth --------------------

        [TestMethod]
        public async Task Certificate_Get_success_after_create()
        {
            var acc = await CreateUser("cert_get_ok");
            var created = await Sut.createCertificateAuthFromCSR(TestMain.ctx, acc.id, Csr());
            Assert.IsTrue(created.IsSuccess());

            var get = await Sut.getCertificateAuth(TestMain.ctx, acc.id, created.Value.id);
            Assert.IsTrue(get.IsSuccess(), get.Error?.MessageText);
            Assert.AreEqual(created.Value.id, get.Value.id);
            Assert.AreEqual(created.Value.serialNumber, get.Value.serialNumber);
            Assert.IsFalse(get.Value.isRevoked);
        }

        [TestMethod]
        public async Task Certificate_Get_fail_bad_ids()
        {
            var acc = await CreateUser("cert_get_bad");
            var created = await Sut.createCertificateAuthFromCSR(TestMain.ctx, acc.id, Csr());
            Assert.IsTrue(created.IsSuccess());

            var badAcc = await Sut.getCertificateAuth(TestMain.ctx, "missing-account", created.Value.id);
            Assert.IsTrue(badAcc.IsFailed());

            var badAuth = await Sut.getCertificateAuth(TestMain.ctx, acc.id, "missing-auth");
            Assert.IsTrue(badAuth.IsFailed());
        }

        // -------------------- revokeCertificate --------------------

        [TestMethod]
        public async Task Certificate_Revoke_success_and_persists()
        {
            var acc = await CreateUser("cert_revoke_ok");
            var created = await Sut.createCertificateAuthFromCSR(TestMain.ctx, acc.id, Csr());
            Assert.IsTrue(created.IsSuccess());

            var reason = "Key compromised";
            var revoke = await Sut.revokeCertificate(
                TestMain.ctx, acc.id, created.Value.id, created.Value.etag, reason);

            Assert.IsTrue(revoke.IsSuccess(), revoke.Error?.MessageText);
            Assert.IsTrue(revoke.Value.isRevoked);
            Assert.AreEqual(reason, revoke.Value.revocationReason);
            Assert.AreNotEqual(default, revoke.Value.revokedAt);

            // ETag/LastUpdate változhat
            Assert.AreNotEqual(created.Value.etag, revoke.Value.etag);
            Assert.IsTrue(revoke.Value.LastUpdate >= created.Value.LastUpdate);

            // read-back
            var get = await Sut.getCertificateAuth(TestMain.ctx, acc.id, created.Value.id);
            Assert.IsTrue(get.IsSuccess());
            Assert.IsTrue(get.Value.isRevoked);
            Assert.AreEqual(reason, get.Value.revocationReason);
        }

        [TestMethod]
        public async Task Certificate_Revoke_fail_wrong_etag_or_ids_or_reason()
        {
            var acc = await CreateUser("cert_revoke_fail");
            var created = await Sut.createCertificateAuthFromCSR(TestMain.ctx, acc.id, Csr());
            Assert.IsTrue(created.IsSuccess());

            // rossz etag
            var wrongEtag = await Sut.revokeCertificate(TestMain.ctx, acc.id, created.Value.id, etag: "WRONG", reason: "reason");
            Assert.IsTrue(wrongEtag.IsFailed(), "Wrong ETag should fail.");

            // rossz account
            var wrongAcc = await Sut.revokeCertificate(TestMain.ctx, "missing-account", created.Value.id, created.Value.etag, "reason");
            Assert.IsTrue(wrongAcc.IsFailed());

            // rossz auth id
            var wrongAuth = await Sut.revokeCertificate(TestMain.ctx, acc.id, "missing-auth", created.Value.etag, "reason");
            Assert.IsTrue(wrongAuth.IsFailed());

            // üres reason
            var emptyReason = await Sut.revokeCertificate(TestMain.ctx, acc.id, created.Value.id, created.Value.etag, "");
            Assert.IsTrue(emptyReason.IsFailed(), "Empty revocation reason should fail.");
        }

        [TestMethod]
        public async Task Certificate_Revoke_fail_double_revoke()
        {
            var acc = await CreateUser("cert_double_revoke");
            var created = await Sut.createCertificateAuthFromCSR(TestMain.ctx, acc.id, Csr());
            Assert.IsTrue(created.IsSuccess());

            var ok = await Sut.revokeCertificate(TestMain.ctx, acc.id, created.Value.id, created.Value.etag, "first");
            Assert.IsTrue(ok.IsSuccess(), ok.Error?.MessageText);

            // második próbálkozás – elvárt: FAIL (már visszavonva)
            var again = await Sut.revokeCertificate(TestMain.ctx, acc.id, ok.Value.id, ok.Value.etag, "second");
            Assert.IsTrue(again.IsFailed(), "Revoking an already revoked certificate should fail.");
        }

        [TestMethod]
        public async Task Certificate_ValidityWindow_includes_Now_with_clock_tolerance()
        {
            var acc = await CreateUser("cert_valid_window");
            var created = await Sut.createCertificateAuthFromCSR(TestMain.ctx, acc.id, Csr());
            Assert.IsTrue(created.IsSuccess(), created.Error?.MessageText);

            var cert = created.Value;

            // sanity
            Assert.IsTrue(cert.validUntil > cert.validFrom, "validUntil must be after validFrom");

            // A szolgáltatás tipikusan UTC-t használ; használjunk UtcNow-t.
            var now = DateTime.UtcNow;

            // Kicsi tolerancia (~2 perc) a CA/tesztgép óraszinkron eltérésére.
            var fromTol = cert.validFrom.AddMinutes(-2);
            var untilTol = cert.validUntil.AddMinutes(2);

            Assert.IsTrue(fromTol <= now && now <= untilTol,
                $"Now ({now:o}) must be within [{cert.validFrom:o}, {cert.validUntil:o}] " +
                $"(with ±2m tolerance).");
        }

        [TestMethod]
        public async Task Certificate_Thumbprint_is_uppercase_hex_with_fixed_length()
        {
            var acc = await CreateUser("cert_thumb_format");
            var created = await Sut.createCertificateAuthFromCSR(TestMain.ctx, acc.id, Csr());
            Assert.IsTrue(created.IsSuccess(), created.Error?.MessageText);

            var thumb = created.Value.certificateThumbprint;

            const int ExpectedHexLen = 64;
            var regex = new Regex(@"^[0-9A-F]+$");

            Assert.IsFalse(string.IsNullOrWhiteSpace(thumb), "Thumbprint must be non-empty.");
            Assert.AreEqual(ExpectedHexLen, thumb.Length,
                $"Thumbprint must be {ExpectedHexLen} hex chars (got {thumb.Length}).");
            Assert.IsTrue(regex.IsMatch(thumb),
                "Thumbprint must contain only uppercase hexadecimal characters [0-9A-F].");
        }

    }
}
