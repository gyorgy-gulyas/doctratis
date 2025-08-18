using Microsoft.Extensions.DependencyInjection;

namespace IAM.Identities.Tests
{
    [TestClass]
    public class IdentityAdminIF_v1_Acccount_AD_Tests
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

        private async Task<IIdentityAdminIF_v1.LdapDomainDTO> CreateLdapDomain(
            string name = "corp.local",
            string netbios = "CORP",
            bool ldaps = true)
        {
            var dto = new IIdentityAdminIF_v1.LdapDomainDTO
            {
                name = name,
                description = "Main domain",
                netbiosName = netbios,
                baseDn = "DC=corp,DC=local",
                useSecureLdap = ldaps,
                serviceAccountUser = "svc_ldap",
                serviceAccountPassword = "secret",
                domainControllers = new()
                {
                    new IIdentityAdminIF_v1.LdapDomainDTO.DomainController { host = "dc1."+name, port = ldaps ? 636 : 389 },
                    new IIdentityAdminIF_v1.LdapDomainDTO.DomainController { host = "dc2."+name, port = ldaps ? 636 : 389 },
                }
            };
            var reg = await Sut.RegisterLdapDomain(TestMain.ctx, dto);
            Assert.IsTrue(reg.IsSuccess(), reg.Error?.MessageText);
            return reg.Value;
        }

        private static IIdentityAdminIF_v1.TwoFactorConfigurationDTO Tfa(
            bool enabled = false,
            IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods method = IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP,
            string phone = null,
            string email = null)
            => new IIdentityAdminIF_v1.TwoFactorConfigurationDTO
            {
                enabled = enabled,
                method = method,
                phoneNumber = phone,
                email = email
            };

        // -------------------- createADAuth --------------------

        [TestMethod]
        public async Task ADAuth_Create_success_and_LdapName_populated()
        {
            var acc = await CreateUser("ad_user_ok");
            var ldap = await CreateLdapDomain(name: "corp.local", netbios: "CORP");

            var create = await Sut.createADAuth(
                TestMain.ctx,
                accountId: acc.id,
                ldapDomainId: ldap.id,
                adUsername: "john.smith",
                twoFactor: Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP));

            Assert.IsTrue(create.IsSuccess(), create.Error?.MessageText);
            Assert.IsFalse(string.IsNullOrEmpty(create.Value.id));
            Assert.AreEqual(ldap.id, create.Value.LdapDomainId);
            Assert.AreEqual("corp.local", create.Value.LdapDomainName, "getADAuth/Convert során a névnek ki kell töltődnie");
            Assert.AreEqual("john.smith", create.Value.userName);
            Assert.IsTrue(create.Value.isActive);
        }

        [TestMethod]
        public async Task ADAuth_Create_fail_missing_or_invalid_arguments()
        {
            var acc = await CreateUser("ad_user_bad");
            var ldap = await CreateLdapDomain();

            // üres felhasználónév
            var emptyUser = await Sut.createADAuth(TestMain.ctx, acc.id, ldap.id, "", Tfa());
            Assert.IsTrue(emptyUser.IsFailed(), "Empty AD username should fail.");

            // rossz account
            var badAcc = await Sut.createADAuth(TestMain.ctx, "missing-account", ldap.id, "user", Tfa());
            Assert.IsTrue(badAcc.IsFailed());

            // rossz ldap domain
            var badDomain = await Sut.createADAuth(TestMain.ctx, acc.id, "missing-ldap", "user", Tfa());
            Assert.IsTrue(badDomain.IsFailed());
        }

        [TestMethod]
        public async Task ADAuth_Create_fail_duplicate_same_domain_and_username()
        {
            var acc = await CreateUser("ad_user_dup");
            var ldap = await CreateLdapDomain();

            var first = await Sut.createADAuth(TestMain.ctx, acc.id, ldap.id, "dup.user", Tfa());
            Assert.IsTrue(first.IsSuccess(), first.Error?.MessageText);

            // Ugyanaz az AD user ugyanabban a domainben -> elvárt: FAIL
            var second = await Sut.createADAuth(TestMain.ctx, acc.id, ldap.id, "dup.user", Tfa());
            Assert.IsTrue(second.IsFailed(), "Duplicate AD user in same domain should fail.");
        }

        [TestMethod]
        public async Task ADAuth_Create_case_insensitive_username_normalization()
        {
            var acc = await CreateUser("ad_user_case");
            var ldap = await CreateLdapDomain();

            var first = await Sut.createADAuth(TestMain.ctx, acc.id, ldap.id, "UPPER.User", Tfa());
            Assert.IsTrue(first.IsSuccess(), first.Error?.MessageText);

            // csak a case különbözik -> elvárt: FAIL (case-insensitive egyediség)
            var second = await Sut.createADAuth(TestMain.ctx, acc.id, ldap.id, "upper.user", Tfa());
            Assert.IsTrue(second.IsFailed(), "Username uniqueness should be case-insensitive.");
        }

        // -------------------- getADAuth --------------------

        [TestMethod]
        public async Task ADAuth_Get_success_with_LdapDomainName()
        {
            var acc = await CreateUser("ad_get_ok");
            var ldap = await CreateLdapDomain(name: "lab.local", netbios: "LAB");
            var created = await Sut.createADAuth(TestMain.ctx, acc.id, ldap.id, "labuser", Tfa());
            Assert.IsTrue(created.IsSuccess());

            var get = await Sut.getADAuth(TestMain.ctx, acc.id, created.Value.id);
            Assert.IsTrue(get.IsSuccess(), get.Error?.MessageText);
            Assert.AreEqual("lab.local", get.Value.LdapDomainName);
            Assert.AreEqual("labuser", get.Value.userName);
            Assert.AreEqual(created.Value.id, get.Value.id);
        }

        [TestMethod]
        public async Task ADAuth_Get_fail_bad_ids()
        {
            var acc = await CreateUser("ad_get_bad");
            var ldap = await CreateLdapDomain();
            var created = await Sut.createADAuth(TestMain.ctx, acc.id, ldap.id, "whoami", Tfa());
            Assert.IsTrue(created.IsSuccess());

            var badAcc = await Sut.getADAuth(TestMain.ctx, "missing-account", created.Value.id);
            Assert.IsTrue(badAcc.IsFailed());

            var badAuth = await Sut.getADAuth(TestMain.ctx, acc.id, "missing-auth");
            Assert.IsTrue(badAuth.IsFailed());
        }

        // -------------------- setTwoFactorOnADAuth --------------------

        [TestMethod]
        public async Task ADAuth_SetTwoFactor_TOTP_success_and_persists()
        {
            var acc = await CreateUser("ad_2fa_totp");
            var ldap = await CreateLdapDomain();
            var created = await Sut.createADAuth(TestMain.ctx, acc.id, ldap.id, "tuser", Tfa());
            Assert.IsTrue(created.IsSuccess());

            var set = await Sut.setTwoFactorOnADAuth(
                TestMain.ctx,
                accountId: acc.id,
                authId: created.Value.id,
                etag: created.Value.etag,
                twoFactor: Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP));

            Assert.IsTrue(set.IsSuccess(), set.Error?.MessageText);
            Assert.IsTrue(set.Value.twoFactor.enabled);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP, set.Value.twoFactor.method);

            // read-back
            var get = await Sut.getADAuth(TestMain.ctx, acc.id, created.Value.id);
            Assert.IsTrue(get.IsSuccess());
            Assert.IsTrue(get.Value.twoFactor.enabled);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP, get.Value.twoFactor.method);
        }

        [TestMethod]
        public async Task ADAuth_SetTwoFactor_SMS_requires_phone()
        {
            var acc = await CreateUser("ad_2fa_sms");
            var ldap = await CreateLdapDomain();
            var created = await Sut.createADAuth(TestMain.ctx, acc.id, ldap.id, "smsuser", Tfa());
            Assert.IsTrue(created.IsSuccess());

            // hiányzó phone -> fail
            var bad = await Sut.setTwoFactorOnADAuth(TestMain.ctx, acc.id, created.Value.id, created.Value.etag,
                Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.SMS, phone: null));
            Assert.IsTrue(bad.IsFailed(), "SMS 2FA requires phone number.");

            // helyes phone -> success
            var ok = await Sut.setTwoFactorOnADAuth(TestMain.ctx, acc.id, created.Value.id, created.Value.etag,
                Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.SMS, phone: "+361234567"));
            Assert.IsTrue(ok.IsSuccess(), ok.Error?.MessageText);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.SMS, ok.Value.twoFactor.method);
            Assert.AreEqual("+361234567", ok.Value.twoFactor.phoneNumber);
        }

        [TestMethod]
        public async Task ADAuth_SetTwoFactor_Email_sets_email_target()
        {
            var acc = await CreateUser("ad_2fa_email");
            var ldap = await CreateLdapDomain();
            var created = await Sut.createADAuth(TestMain.ctx, acc.id, ldap.id, "mailuser", Tfa());
            Assert.IsTrue(created.IsSuccess());

            var set = await Sut.setTwoFactorOnADAuth(TestMain.ctx, acc.id, created.Value.id, created.Value.etag,
                Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.Email, email: "2fa@example.com"));
            Assert.IsTrue(set.IsSuccess(), set.Error?.MessageText);
            Assert.IsTrue(set.Value.twoFactor.enabled);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.Email, set.Value.twoFactor.method);
            Assert.AreEqual("2fa@example.com", set.Value.twoFactor.email);
        }

        [TestMethod]
        public async Task ADAuth_SetTwoFactor_idempotent_with_same_payload()
        {
            var acc = await CreateUser("ad_2fa_idem");
            var ldap = await CreateLdapDomain();
            var created = await Sut.createADAuth(TestMain.ctx, acc.id, ldap.id, "idemuser", Tfa());
            Assert.IsTrue(created.IsSuccess());

            var req = Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP);

            var set1 = await Sut.setTwoFactorOnADAuth(TestMain.ctx, acc.id, created.Value.id, created.Value.etag, req);
            Assert.IsTrue(set1.IsSuccess(), set1.Error?.MessageText);

            var set2 = await Sut.setTwoFactorOnADAuth(TestMain.ctx, acc.id, set1.Value.id, set1.Value.etag, req);
            Assert.IsTrue(set2.IsSuccess(), set2.Error?.MessageText);

            // állapot változatlan marad
            Assert.IsTrue(set2.Value.twoFactor.enabled);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP, set2.Value.twoFactor.method);
        }

        [TestMethod]
        public async Task ADAuth_SetTwoFactor_fail_wrong_etag_or_ids()
        {
            var acc = await CreateUser("ad_2fa_wrong");
            var ldap = await CreateLdapDomain();
            var created = await Sut.createADAuth(TestMain.ctx, acc.id, ldap.id, "wronguser", Tfa());
            Assert.IsTrue(created.IsSuccess());

            var req = Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP);

            var wrongEtag = await Sut.setTwoFactorOnADAuth(TestMain.ctx, acc.id, created.Value.id, etag: "WRONG", req);
            Assert.IsTrue(wrongEtag.IsFailed());

            var wrongAcc = await Sut.setTwoFactorOnADAuth(TestMain.ctx, "missing-acc", created.Value.id, created.Value.etag, req);
            Assert.IsTrue(wrongAcc.IsFailed());

            var wrongAuth = await Sut.setTwoFactorOnADAuth(TestMain.ctx, acc.id, "missing-auth", created.Value.etag, req);
            Assert.IsTrue(wrongAuth.IsFailed());
        }

        [TestMethod]
        public async Task ADAuth_Create_fail_duplicate_username_and_domain_across_different_accounts()
        {
            // Arrange
            var acc1 = await CreateUser("ad_cross_acc_1");
            var acc2 = await CreateUser("ad_cross_acc_2");
            var ldap = await CreateLdapDomain(name: "corp.local", netbios: "CORP");

            // Act 1: első account sikeresen felveszi az ADAuth-ot
            var first = await Sut.createADAuth(TestMain.ctx, acc1.id, ldap.id, "dup.user", Tfa());
            Assert.IsTrue(first.IsSuccess(), first.Error?.MessageText);

            // Act 2: második account ugyanazzal a (domain, username) párral -> FAIL
            var second = await Sut.createADAuth(TestMain.ctx, acc2.id, ldap.id, "dup.user", Tfa());

            // Assert
            Assert.IsTrue(second.IsFailed(), "Same (domain, username) must be unique globally across accounts.");
            // opcionális: ellenőrizd a státuszt/üzenetet
            // Assert.AreEqual(ServiceKit.Net.Statuses.BadRequest, second.Error.Status);
            Assert.IsTrue(
                second.Error.MessageText.Contains("already", StringComparison.OrdinalIgnoreCase) ||
                second.Error.MessageText.Contains("exists", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public async Task ADAuth_Create_fail_duplicate_username_and_domain_case_insensitive_across_accounts()
        {
            // Arrange
            var acc1 = await CreateUser("ad_cross_ci_1");
            var acc2 = await CreateUser("ad_cross_ci_2");
            var ldap = await CreateLdapDomain(); // corp.local

            // Act 1: első account UPPER case username
            var first = await Sut.createADAuth(TestMain.ctx, acc1.id, ldap.id, "UPPER.User", Tfa());
            Assert.IsTrue(first.IsSuccess(), first.Error?.MessageText);

            // Act 2: második account ugyanaz lowercase-ben -> FAIL (case-insensitive)
            var second = await Sut.createADAuth(TestMain.ctx, acc2.id, ldap.id, "upper.user", Tfa());

            // Assert
            Assert.IsTrue(second.IsFailed(), "Username+domain uniqueness should be case-insensitive across accounts.");
        }

    }
}
