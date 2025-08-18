using Microsoft.Extensions.DependencyInjection;

namespace IAM.Identities.Tests
{
    [TestClass]
    public class IdentityAdminIF_v1_Acccount_KAU_Tests
    {
        private IIdentityAdminIF_v1 Sut =>
            TestMain.ServiceProvider.GetRequiredService<IIdentityAdminIF_v1>();

        [TestInitialize]
        public async Task Setup()
        {
            await TestMain.DeleteAllData();
        }

        private async Task<IIdentityAdminIF_v1.AccountDTO> CreateUser(string name)
        {
            var res = await Sut.createAccount(TestMain.ctx, name, IIdentityAdminIF_v1.AccountTypes.User);
            Assert.IsTrue(res.IsSuccess(), res.Error?.MessageText);
            return res.Value;
        }

        private static IIdentityAdminIF_v1.TwoFactorConfigurationDTO Tfa(
            bool enabled = false,
            IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods method = IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.Email,
            string phone = null,
            string email = null)
            => new IIdentityAdminIF_v1.TwoFactorConfigurationDTO
            {
                enabled = enabled,
                method = method,
                phoneNumber = phone,
                email = email
            };

        // -------------------- createKAUAuth --------------------

        [TestMethod]
        public async Task KAUAuth_Create_success_minimal()
        {
            var acc = await CreateUser("kau_user_ok");

            var create = await Sut.createKAUAuth(
                TestMain.ctx,
                accountId: acc.id,
                kauUserId: "KAU-123456",
                twoFactor: Tfa(enabled: false));

            Assert.IsTrue(create.IsSuccess(), create.Error?.MessageText);
            Assert.IsFalse(string.IsNullOrEmpty(create.Value.id));
            Assert.IsFalse(string.IsNullOrEmpty(create.Value.etag));
            Assert.AreNotEqual(DateTime.MinValue, create.Value.LastUpdate);
            Assert.AreEqual("KAU-123456", create.Value.KAUUserId);
            Assert.IsTrue(create.Value.isActive);
            // legalName / email opcionálisak – nem kötelező ellenőrizni
        }

        [TestMethod]
        public async Task KAUAuth_Create_fail_missing_or_invalid_arguments()
        {
            var acc = await CreateUser("kau_user_bad");

            // üres KAU id
            var emptyKau = await Sut.createKAUAuth(TestMain.ctx, acc.id, "", Tfa());
            Assert.IsTrue(emptyKau.IsFailed(), "Empty KAU user id should fail.");

            // ismeretlen account
            var badAcc = await Sut.createKAUAuth(TestMain.ctx, "missing-account", "KAU-ABC", Tfa());
            Assert.IsTrue(badAcc.IsFailed());
        }

        [TestMethod]
        public async Task KAUAuth_Create_fail_duplicate_same_account()
        {
            var acc = await CreateUser("kau_user_dup_same");

            var first = await Sut.createKAUAuth(TestMain.ctx, acc.id, "KAU-DUP", Tfa());
            Assert.IsTrue(first.IsSuccess(), first.Error?.MessageText);

            var second = await Sut.createKAUAuth(TestMain.ctx, acc.id, "KAU-DUP", Tfa());
            Assert.IsTrue(second.IsFailed(), "Duplicate KAU user id on same account should fail.");
        }

        [TestMethod]
        public async Task KAUAuth_Create_fail_duplicate_across_accounts_global_uniqueness()
        {
            var acc1 = await CreateUser("kau_user_acc1");
            var acc2 = await CreateUser("kau_user_acc2");

            var first = await Sut.createKAUAuth(TestMain.ctx, acc1.id, "KAU-GLOB-1", Tfa());
            Assert.IsTrue(first.IsSuccess(), first.Error?.MessageText);

            var second = await Sut.createKAUAuth(TestMain.ctx, acc2.id, "KAU-GLOB-1", Tfa());
            Assert.IsTrue(second.IsFailed(), "KAU user id must be globally unique across accounts.");
        }

        // -------------------- getKAUAuth --------------------

        [TestMethod]
        public async Task KAUAuth_Get_success()
        {
            var acc = await CreateUser("kau_get_ok");

            var created = await Sut.createKAUAuth(TestMain.ctx, acc.id, "KAU-GET-1", Tfa());
            Assert.IsTrue(created.IsSuccess());

            var get = await Sut.getKAUAuth(TestMain.ctx, acc.id, created.Value.id);
            Assert.IsTrue(get.IsSuccess(), get.Error?.MessageText);
            Assert.AreEqual("KAU-GET-1", get.Value.KAUUserId);
            Assert.AreEqual(created.Value.id, get.Value.id);
        }

        [TestMethod]
        public async Task KAUAuth_Get_fail_bad_ids()
        {
            var acc = await CreateUser("kau_get_bad");

            var created = await Sut.createKAUAuth(TestMain.ctx, acc.id, "KAU-GET-FAIL", Tfa());
            Assert.IsTrue(created.IsSuccess());

            var badAcc = await Sut.getKAUAuth(TestMain.ctx, "missing-account", created.Value.id);
            Assert.IsTrue(badAcc.IsFailed());

            var badAuth = await Sut.getKAUAuth(TestMain.ctx, acc.id, "missing-auth");
            Assert.IsTrue(badAuth.IsFailed());
        }

        // -------------------- setTwoFactorOnKAUAuth --------------------

        [TestMethod]
        public async Task KAUAuth_SetTwoFactor_TOTP_success_and_persists()
        {
            var acc = await CreateUser("kau_2fa_totp");

            var created = await Sut.createKAUAuth(TestMain.ctx, acc.id, "KAU-TOTP-1",
                Tfa(enabled: false));
            Assert.IsTrue(created.IsSuccess());

            var set = await Sut.setTwoFactorOnKAUAuth(
                TestMain.ctx,
                accountId: acc.id,
                authId: created.Value.id,
                etag: created.Value.etag,
                twoFactor: Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP));

            Assert.IsTrue(set.IsSuccess(), set.Error?.MessageText);
            Assert.IsTrue(set.Value.twoFactor.enabled);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP, set.Value.twoFactor.method);

            // read-back
            var get = await Sut.getKAUAuth(TestMain.ctx, acc.id, created.Value.id);
            Assert.IsTrue(get.IsSuccess());
            Assert.IsTrue(get.Value.twoFactor.enabled);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP, get.Value.twoFactor.method);
        }

        [TestMethod]
        public async Task KAUAuth_SetTwoFactor_SMS_requires_phone()
        {
            var acc = await CreateUser("kau_2fa_sms");

            var created = await Sut.createKAUAuth(TestMain.ctx, acc.id, "KAU-SMS-1", Tfa());
            Assert.IsTrue(created.IsSuccess());

            // hiányzó phone -> fail
            var bad = await Sut.setTwoFactorOnKAUAuth(TestMain.ctx, acc.id, created.Value.id, created.Value.etag,
                Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.SMS, phone: null));
            Assert.IsTrue(bad.IsFailed(), "SMS 2FA requires phone number.");

            // helyes phone -> success
            var ok = await Sut.setTwoFactorOnKAUAuth(TestMain.ctx, acc.id, created.Value.id, created.Value.etag,
                Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.SMS, phone: "+361234567"));
            Assert.IsTrue(ok.IsSuccess(), ok.Error?.MessageText);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.SMS, ok.Value.twoFactor.method);
            Assert.AreEqual("+361234567", ok.Value.twoFactor.phoneNumber);
        }

        [TestMethod]
        public async Task KAUAuth_SetTwoFactor_Email_sets_email_target()
        {
            var acc = await CreateUser("kau_2fa_email");

            var created = await Sut.createKAUAuth(TestMain.ctx, acc.id, "KAU-EMAIL-1", Tfa());
            Assert.IsTrue(created.IsSuccess());

            var set = await Sut.setTwoFactorOnKAUAuth(TestMain.ctx, acc.id, created.Value.id, created.Value.etag,
                Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.Email, email: "2fa@example.com"));
            Assert.IsTrue(set.IsSuccess(), set.Error?.MessageText);
            Assert.IsTrue(set.Value.twoFactor.enabled);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.Email, set.Value.twoFactor.method);
            Assert.AreEqual("2fa@example.com", set.Value.twoFactor.email);
        }

        [TestMethod]
        public async Task KAUAuth_SetTwoFactor_idempotent_with_same_payload()
        {
            var acc = await CreateUser("kau_2fa_idem");

            var created = await Sut.createKAUAuth(TestMain.ctx, acc.id, "KAU-IDEM-1", Tfa());
            Assert.IsTrue(created.IsSuccess());

            var req = Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP);

            var set1 = await Sut.setTwoFactorOnKAUAuth(TestMain.ctx, acc.id, created.Value.id, created.Value.etag, req);
            Assert.IsTrue(set1.IsSuccess(), set1.Error?.MessageText);

            var set2 = await Sut.setTwoFactorOnKAUAuth(TestMain.ctx, acc.id, set1.Value.id, set1.Value.etag, req);
            Assert.IsTrue(set2.IsSuccess(), set2.Error?.MessageText);

            // állapot változatlan marad
            Assert.IsTrue(set2.Value.twoFactor.enabled);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP, set2.Value.twoFactor.method);
        }

        [TestMethod]
        public async Task KAUAuth_SetTwoFactor_toggle_disable_persists()
        {
            var acc = await CreateUser("kau_2fa_toggle");

            var created = await Sut.createKAUAuth(TestMain.ctx, acc.id, "KAU-TGL-1", Tfa());
            Assert.IsTrue(created.IsSuccess());

            // enable
            var on = await Sut.setTwoFactorOnKAUAuth(TestMain.ctx, acc.id, created.Value.id, created.Value.etag,
                Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP));
            Assert.IsTrue(on.IsSuccess());

            // disable
            var off = await Sut.setTwoFactorOnKAUAuth(TestMain.ctx, acc.id, on.Value.id, on.Value.etag,
                Tfa(enabled: false, method: on.Value.twoFactor.method, phone: on.Value.twoFactor.phoneNumber, email: on.Value.twoFactor.email));
            Assert.IsTrue(off.IsSuccess(), off.Error?.MessageText);
            Assert.IsFalse(off.Value.twoFactor.enabled);

            // read-back
            var get = await Sut.getKAUAuth(TestMain.ctx, acc.id, created.Value.id);
            Assert.IsTrue(get.IsSuccess());
            Assert.IsFalse(get.Value.twoFactor.enabled);
        }

        [TestMethod]
        public async Task KAUAuth_SetTwoFactor_fail_wrong_etag_or_ids()
        {
            var acc = await CreateUser("kau_2fa_wrong");

            var created = await Sut.createKAUAuth(TestMain.ctx, acc.id, "KAU-WRONG-1", Tfa());
            Assert.IsTrue(created.IsSuccess());

            var req = Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP);

            var wrongEtag = await Sut.setTwoFactorOnKAUAuth(TestMain.ctx, acc.id, created.Value.id, etag: "WRONG", req);
            Assert.IsTrue(wrongEtag.IsFailed());

            var wrongAcc = await Sut.setTwoFactorOnKAUAuth(TestMain.ctx, "missing-acc", created.Value.id, created.Value.etag, req);
            Assert.IsTrue(wrongAcc.IsFailed());

            var wrongAuth = await Sut.setTwoFactorOnKAUAuth(TestMain.ctx, acc.id, "missing-auth", created.Value.etag, req);
            Assert.IsTrue(wrongAuth.IsFailed());
        }
    }
}
