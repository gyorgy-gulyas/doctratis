using Core.Test.Mock;
using Microsoft.Extensions.DependencyInjection;
using ServiceKit.Net.Communicators;

namespace IAM.Identities.Tests
{
    [TestClass]
    public partial class IdentityAdminIF_v1_Acccount_Email_Tests
    {
        private IIdentityAdminIF_v1 Sut =>
           TestMain.ServiceProvider.GetRequiredService<IIdentityAdminIF_v1>();

        [TestInitialize]
        public async Task Setup()
        {
            await TestMain.DeleteAllData();
        }

        // --------- helpers ---------------------------------------------------

        private async Task<IIdentityAdminIF_v1.AccountDTO> CreateUser(string name = "email_user")
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


        // --------- createtEmailAuth -----------------------------------------

        [TestMethod]
        public async Task EmailAuth_Create_success_minimal()
        {
            var acc = await CreateUser("ea_min");
            var create = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "ea_min@example.com", "InitPassword#123", Tfa(enabled: false));
            Assert.IsTrue(create.IsSuccess(), create.Error?.MessageText);

            var dto = create.Value;
            Assert.AreEqual("ea_min@example.com", dto.email);
            Assert.IsFalse(dto.isEmailConfirmed);
            Assert.IsTrue(dto.isActive);
            Assert.IsFalse(string.IsNullOrEmpty(dto.id));
            Assert.IsFalse(string.IsNullOrEmpty(dto.etag));
            Assert.AreNotEqual(DateTime.MinValue, dto.LastUpdate);
        }

        [TestMethod]
        public async Task EmailAuth_Create_fail_duplicate_email_same_account()
        {
            var acc = await CreateUser("dup_user");
            var first = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "dup@example.com", "InitPassword#123", Tfa());
            Assert.IsTrue(first.IsSuccess(), first.Error?.MessageText);

            var second = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "dup@example.com", "InitPassword#123", Tfa());
            Assert.IsTrue(second.IsFailed(), "Duplicate email auth on same account should fail.");
            Assert.AreEqual(ServiceKit.Net.Statuses.BadRequest, second.Error.Status);
        }

        // --------- getEmailAuth ---------------------------------------------

        [TestMethod]
        public async Task EmailAuth_Get_success()
        {
            var acc = await CreateUser("ea_get");
            var create = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "ea_get@example.com", "InitPassword#123", Tfa());
            Assert.IsTrue(create.IsSuccess());

            var get = await Sut.getEmailAuth(TestMain.ctx, acc.id, create.Value.id);
            Assert.IsTrue(get.IsSuccess(), get.Error?.MessageText);
            Assert.AreEqual("ea_get@example.com", get.Value.email);
            Assert.AreEqual(create.Value.id, get.Value.id);
        }

        [TestMethod]
        public async Task EmailAuth_Get_fail_notfound()
        {
            var acc = await CreateUser("ea_missing");
            var get = await Sut.getEmailAuth(TestMain.ctx, acc.id, authId: "does-not-exist");
            Assert.IsTrue(get.IsFailed());
            Assert.AreEqual(ServiceKit.Net.Statuses.NotFound, get.Error.Status);
        }

        // --------- listAuthsForAccount --------------------------------------

        [TestMethod]
        public async Task Auth_List_for_account_success()
        {
            var acc = await CreateUser("ea_list");
            // kezdetben üres
            var empty = await Sut.listAuthsForAccount(TestMain.ctx, acc.id);
            Assert.IsTrue(empty.IsSuccess());
            Assert.AreEqual(0, empty.Value.Count);

            // hozz létre egy email auth-ot
            var create = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "ea_list@example.com", "InitPassword#123", Tfa());
            Assert.IsTrue(create.IsSuccess());

            var list = await Sut.listAuthsForAccount(TestMain.ctx, acc.id);
            Assert.IsTrue(list.IsSuccess());
            Assert.AreEqual(1, list.Value.Count);
            Assert.AreEqual(IIdentityAdminIF_v1.AuthDTO.Methods.Email, list.Value[0].method);
            Assert.IsTrue(list.Value[0].isActive);
        }

        [TestMethod]
        public async Task Auth_List_for_account_success_unknown_account_returns_empty_or_fail()
        {
            var list = await Sut.listAuthsForAccount(TestMain.ctx, "missing-account-id");
            // Egyes repo-k üres listát adnak vissza, mások NotFound-ot – bármelyik elfogadható.
            if (list.IsSuccess())
            {
                Assert.AreEqual(0, list.Value.Count);
            }
            else
            {
                Assert.AreEqual(ServiceKit.Net.Statuses.NotFound, list.Error.Status);
                Assert.IsTrue(list.IsFailed());
            }
        }

        // --------- setActiveForAuth -----------------------------------------

        [TestMethod]
        public async Task Auth_SetActive_toggle_success_and_persists()
        {
            var acc = await CreateUser("ea_active");
            var create = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "ea_active@example.com", "InitPassword#123", Tfa());
            Assert.IsTrue(create.IsSuccess());

            // deactivate
            var off = await Sut.setActiveForAuth(TestMain.ctx, acc.id, create.Value.id, create.Value.etag, isActive: false);
            Assert.IsTrue(off.IsSuccess(), off.Error?.MessageText);
            Assert.IsFalse(off.Value.isActive);

            var readOff = await Sut.getEmailAuth(TestMain.ctx, acc.id, create.Value.id);
            Assert.IsTrue(readOff.IsSuccess());
            Assert.IsFalse(readOff.Value.isActive);

            // reactivate
            var on = await Sut.setActiveForAuth(TestMain.ctx, acc.id, readOff.Value.id, readOff.Value.etag, isActive: true);
            Assert.IsTrue(on.IsSuccess(), on.Error?.MessageText);
            Assert.IsTrue(on.Value.isActive);

            var readOn = await Sut.getEmailAuth(TestMain.ctx, acc.id, on.Value.id);
            Assert.IsTrue(readOn.IsSuccess());
            Assert.IsTrue(readOn.Value.isActive);
        }

        [TestMethod]
        public async Task Auth_SetActive_fail_bad_ids()
        {
            var acc = await CreateUser("ea_active_bad");
            var create = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "ea_active_bad@example.com", "InitPassword#123", Tfa());
            Assert.IsTrue(create.IsSuccess());

            var badAcc = await Sut.setActiveForAuth(TestMain.ctx, "missing-account", create.Value.id, create.Value.etag, true);
            Assert.IsTrue(badAcc.IsFailed());

            var badAuth = await Sut.setActiveForAuth(TestMain.ctx, acc.id, "missing-auth", create.Value.etag, true);
            Assert.IsTrue(badAuth.IsFailed());
            Assert.AreEqual(ServiceKit.Net.Statuses.NotFound, badAuth.Error.Status);
        }

        // --------- changePasswordOnEmailAuth --------------------------------

        [TestMethod]
        public async Task EmailAuth_ChangePassword_success_updates_etag_and_lastupdate()
        {
            var acc = await CreateUser("ea_pw");
            var create = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "ea_pw@example.com", "InitPassword#123", Tfa());
            Assert.IsTrue(create.IsSuccess());

            var before = create.Value;
            var chg = await Sut.changePasswordOnEmailAuth(TestMain.ctx, acc.id, before.id, before.etag, "NewPassword#456");
            Assert.IsTrue(chg.IsSuccess(), chg.Error?.MessageText);

            var after = chg.Value;
            Assert.AreNotEqual(before.etag, after.etag, "ETag should change after password update.");
            Assert.IsTrue(after.LastUpdate >= before.LastUpdate);
        }

        [TestMethod]
        public async Task EmailAuth_ChangePassword_fail_wrong_etag()
        {
            var acc = await CreateUser("ea_pw_bad");
            var create = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "ea_pw_bad@example.com", "InitPassword#123", Tfa());
            Assert.IsTrue(create.IsSuccess());

            var fail = await Sut.changePasswordOnEmailAuth(TestMain.ctx, acc.id, create.Value.id, etag: "WRONG-ETAG", "NewPassword#456");
            Assert.IsTrue(fail.IsFailed(), "Wrong ETag should fail.");
            Assert.AreEqual(ServiceKit.Net.Statuses.BadRequest, fail.Error.Status);
        }

        // --------- setTwoFactorOnEmailAuth ----------------------------------

        [TestMethod]
        public async Task EmailAuth_SetTwoFactor_enable_TOTP_success()
        {
            var acc = await CreateUser("ea_tfa_totp");
            var create = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "ea_tfa_totp@example.com", "InitPassword#123", Tfa());
            Assert.IsTrue(create.IsSuccess());

            var req = Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP);
            var set = await Sut.setTwoFactorOnEmailAuth(TestMain.ctx, acc.id, create.Value.id, create.Value.etag, req);
            Assert.IsTrue(set.IsSuccess(), set.Error?.MessageText);
            Assert.IsTrue(set.Value.twoFactor.enabled);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP, set.Value.twoFactor.method);
        }

        [TestMethod]
        public async Task EmailAuth_SetTwoFactor_switch_to_SMS_requires_phone()
        {
            var acc = await CreateUser("ea_tfa_sms");
            var create = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "ea_tfa_sms@example.com", "InitPassword#123", Tfa());
            Assert.IsTrue(create.IsSuccess());

            // hiányzó phone -> elvárt: fail
            var badReq = Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.SMS, phone: null);
            var bad = await Sut.setTwoFactorOnEmailAuth(TestMain.ctx, acc.id, create.Value.id, create.Value.etag, badReq);
            Assert.IsTrue(bad.IsFailed(), "SMS 2FA requires phone number.");

            // helyes phone -> success
            var okReq = Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.SMS, phone: "+361234567");
            var ok = await Sut.setTwoFactorOnEmailAuth(TestMain.ctx, acc.id, create.Value.id, create.Value.etag, okReq);
            Assert.IsTrue(ok.IsSuccess(), ok.Error?.MessageText);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.SMS, ok.Value.twoFactor.method);
            Assert.AreEqual("+361234567", ok.Value.twoFactor.phoneNumber);
        }

        [TestMethod]
        public async Task EmailAuth_SetTwoFactor_email_method_sets_email_target()
        {
            var acc = await CreateUser("ea_tfa_email");
            var create = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "ea_tfa_email@example.com", "InitPassword#123", Tfa());
            Assert.IsTrue(create.IsSuccess());

            var req = Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.Email, email: "2fa@example.com");
            var set = await Sut.setTwoFactorOnEmailAuth(TestMain.ctx, acc.id, create.Value.id, create.Value.etag, req);
            Assert.IsTrue(set.IsSuccess(), set.Error?.MessageText);
            Assert.IsTrue(set.Value.twoFactor.enabled);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.Email, set.Value.twoFactor.method);
            Assert.AreEqual("2fa@example.com", set.Value.twoFactor.email);
        }

        [TestMethod]
        public async Task EmailAuth_SetTwoFactor_fail_wrong_etag_or_ids()
        {
            var acc = await CreateUser("ea_tfa_bad");
            var create = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "ea_tfa_bad@example.com", "InitPassword#123", Tfa());
            Assert.IsTrue(create.IsSuccess());

            var req = Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP);

            var wrongEtag = await Sut.setTwoFactorOnEmailAuth(TestMain.ctx, acc.id, create.Value.id, etag: "WRONG", req);
            Assert.IsTrue(wrongEtag.IsFailed());

            var wrongAcc = await Sut.setTwoFactorOnEmailAuth(TestMain.ctx, "missing-acc", create.Value.id, create.Value.etag, req);
            Assert.IsTrue(wrongAcc.IsFailed());

            var wrongAuth = await Sut.setTwoFactorOnEmailAuth(TestMain.ctx, acc.id, "missing-auth", create.Value.etag, req);
            Assert.IsTrue(wrongAuth.IsFailed());
        }

        // --------- confirmEmail ---------------------------------------------

        [TestMethod]
        public async Task EmailAuth_ConfirmEmail_fail_invalid_token()
        {
            var res = await Sut.confirmEmail(TestMain.ctx, token: "definitely-not-valid");
            Assert.IsTrue(res.IsFailed());
            Assert.AreEqual(ServiceKit.Net.Statuses.BadRequest, res.Error.Status);
        }

        // MEGJEGYZÉS:
        // A sikeres confirmEmail teszthez szükség van egy módra, hogy a létrehozáskor generált
        // e-mail megerősítő tokent a teszt ki tudja olvasni (pl. IEmailTokenRepository / FakeMailer).
        // Ha van ilyen a DI-ben, itt olvasd ki és futtasd a sikeres ellenőrzést.
        [TestMethod]
        public async Task EmailAuth_ConfirmEmail_success_with_captured_token()
        {
            var emailCommunicator = TestMain.ServiceProvider.GetRequiredService<IEmailCommunicator>() as Mock_EmailCommunicator;

            var acc = await CreateUser("ea_confirm");
            var create = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "ea_confirm@example.com", "InitPassword#123", Tfa());
            Assert.IsTrue(create.IsSuccess());

            var mail = emailCommunicator.sentMessages.FirstOrDefault( mail => mail.recipients.Contains("ea_confirm@example.com"));
            Assert.IsNotNull(mail);
            int start = mail.body.IndexOf('<');
            int end = mail.body.IndexOf('>');
            Assert.IsTrue(start >= 0 && end > start);
            string token = mail.body.Substring(start + 1, end - start - 1);

            // 2) Confirm
            var ok = await Sut.confirmEmail(TestMain.ctx, token);
            Assert.IsTrue(ok.IsSuccess(), ok.Error?.MessageText);

            // 3) Read-back: isEmailConfirmed = true
            var get = await Sut.getEmailAuth(TestMain.ctx, acc.id, create.Value.id);
            Assert.IsTrue(get.IsSuccess());
            Assert.IsTrue(get.Value.isEmailConfirmed);
        }

        // =====================================================================
        // 1) Case-insensitive email ütközés
        // =====================================================================

        [TestMethod]
        public async Task EmailAuth_Create_fail_duplicate_email_case_insensitive_on_same_account()
        {
            var acc = await CreateUser("ci_dup");

            var first = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "User@X.com", "ValidPass#1234", Tfa());
            Assert.IsTrue(first.IsSuccess(), first.Error?.MessageText);

            // csak a case különbözik
            var second = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "user@x.com", "ValidPass#1234", Tfa());
            Assert.IsTrue(second.IsFailed(), "Email uniqueness should be case-insensitive within the same account.");
            // opcionálisan:
            // Assert.AreEqual(ServiceKit.Net.Statuses.BadRequest, second.Error.Status);
            Assert.IsTrue(second.Error.MessageText.Contains("already", StringComparison.OrdinalIgnoreCase)
                          || second.Error.MessageText.Contains("exists", StringComparison.OrdinalIgnoreCase));
        }

        // Megjegyzés: ha globális (accountok közötti) egyediség is elvárt,
        // itt létrehozhatsz egy másik accountot és ugyanazzal az email-lel megpróbálod —
        // jelen teszt csak az "azonos account" esetre tesztel.

        // =====================================================================
        // 2) Password policy (PasswordAgent szabályai)
        //    - Létrehozáskor (createtEmailAuth)
        //    - Jelszócserekor (changePasswordOnEmailAuth)
        // =====================================================================

        // A DataTestMethod + DataRow listában több tipikus szabálysértés:
        // - túl rövid
        // - whitespace
        // - hiányzó nagybetű/kisbetű/szám/spec
        // - kevés egyedi karakter
        // - túl hosszú azonos karakter futam
        [DataTestMethod]
        [DataRow("Short#1", "Password must be at least")]       // túl rövid (<12)
        [DataRow("ThisHas Space#1", "must not contain whitespace")]     // whitespace
        [DataRow("alllowercase#1", "uppercase")]                       // nincs nagybetű
        [DataRow("ALLUPPERCASE#1", "lowercase")]                       // nincs kisbetű
        [DataRow("NoDigits####", "digit")]                           // nincs szám
        [DataRow("NoSpec123456", "special")]                         // nincs spec karakter
        [DataRow("Aa1!Aa1!Aa1!", "distinct")]                        // kevés egyedi karakter (<5)
        [DataRow("AAAaaa111!!!", "at least")]               // 4+ azonos egymás után
        public async Task PasswordPolicy_CreateEmailAuth_fails_on_invalid_passwords(string badPassword, string expectMessageContains)
        {
            var acc = await CreateUser("pw_create_fail");
            var res = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "pw_create_fail@example.com", badPassword, Tfa());

            Assert.IsTrue(res.IsFailed(), $"Expected failure for password '{badPassword}'");
            Assert.IsTrue(res.Error.MessageText.Contains("Password", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(res.Error.AdditionalInformation.Contains(expectMessageContains, StringComparison.OrdinalIgnoreCase),
                $"Error should mention: {expectMessageContains}. Actual: {res.Error.MessageText}");
        }

        [TestMethod]
        public async Task PasswordPolicy_CreateEmailAuth_success_on_valid_password()
        {
            var acc = await CreateUser("pw_create_ok");
            // megfelel: >=12, van Upper, lower, digit, spec, nincs whitespace, elég egyedi
            var res = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "pw_create_ok@example.com", "Good#Pass1234", Tfa());
            Assert.IsTrue(res.IsSuccess(), res.Error?.MessageText);
        }

        [DataTestMethod]
        [DataRow("Short#1", "at least")]
        [DataRow("ThisHas Space#1", "whitespace")]
        [DataRow("alllowercase#1", "uppercase")]
        [DataRow("ALLUPPERCASE#1", "lowercase")]
        [DataRow("NoDigits####", "digit")]
        [DataRow("NoSpec123456", "special")]
        [DataRow("Aa1!Aa1!Aa1!", "distinct")]
        [DataRow("AAAaaa111!!!", "at least")]
        public async Task PasswordPolicy_ChangePassword_fails_on_invalid_passwords(string badPassword, string expectMessageContains)
        {
            var acc = await CreateUser("pw_change_fail");
            var created = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "pw_change_fail@example.com", "Good#Pass1234", Tfa());
            Assert.IsTrue(created.IsSuccess(), created.Error?.MessageText);

            var res = await Sut.changePasswordOnEmailAuth(TestMain.ctx, acc.id, created.Value.id, created.Value.etag, badPassword);
            Assert.IsTrue(res.IsFailed(), $"Expected failure for new password '{badPassword}'");
            Assert.IsTrue(res.Error.AdditionalInformation.Contains(expectMessageContains, StringComparison.OrdinalIgnoreCase),
                $"Error should mention: {expectMessageContains}. Actual: {res.Error.MessageText}");
        }

        [TestMethod]
        public async Task PasswordPolicy_ChangePassword_success_and_updates_etag_and_lastupdate()
        {
            var acc = await CreateUser("pw_change_ok");
            var created = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "pw_change_ok@example.com", "Good#Pass1234", Tfa());
            Assert.IsTrue(created.IsSuccess());

            var before = created.Value;
            var ok = await Sut.changePasswordOnEmailAuth(TestMain.ctx, acc.id, before.id, before.etag, "Another#Pass1234");
            Assert.IsTrue(ok.IsSuccess(), ok.Error?.MessageText);
            Assert.AreNotEqual(before.etag, ok.Value.etag);
            Assert.IsTrue(ok.Value.LastUpdate >= before.LastUpdate);
        }

        [TestMethod]
        public async Task PasswordPolicy_ChangePassword_fail_same_as_current_forbidden()
        {
            var acc = await CreateUser("pw_same");
            var created = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "pw_same@example.com", "Good#Pass1234", Tfa());
            Assert.IsTrue(created.IsSuccess());

            // új jelszó = régi jelszó
            var fail = await Sut.changePasswordOnEmailAuth(TestMain.ctx, acc.id, created.Value.id, created.Value.etag, "Good#Pass1234");
            Assert.IsTrue(fail.IsFailed(), "Should reject reusing current password.");
            Assert.IsTrue(fail.Error.MessageText.Contains("different from the current password", StringComparison.OrdinalIgnoreCase)
                          || fail.Error.MessageText.Contains("not match any of the previously used", StringComparison.OrdinalIgnoreCase));
        }

        // (Opcionálisan ide jöhetne Password history multi-step teszt, ha az auth service ténylegesen
        //  karbantartja a history-t; ennek hiányában a fenti "same as current" védi az azonnali visszaállítást.)

        // =====================================================================
        // 3) 2FA idempotencia (EmailAuth) – ugyanaz a beállítás kétszer
        // =====================================================================

        [TestMethod]
        public async Task EmailAuth_SetTwoFactor_idempotent_with_same_payload()
        {
            var acc = await CreateUser("tfa_idem");
            var created = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "tfa_idem@example.com", "Good#Pass1234", Tfa());
            Assert.IsTrue(created.IsSuccess());

            // 1) állítsuk TOTP-ra, enabled = true
            var req = Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP);
            var set1 = await Sut.setTwoFactorOnEmailAuth(TestMain.ctx, acc.id, created.Value.id, created.Value.etag, req);
            Assert.IsTrue(set1.IsSuccess(), set1.Error?.MessageText);
            Assert.IsTrue(set1.Value.twoFactor.enabled);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP, set1.Value.twoFactor.method);

            // 2) ugyanaz a payload ismét – az etag időközben változott; azt használjuk
            var set2 = await Sut.setTwoFactorOnEmailAuth(TestMain.ctx, acc.id, set1.Value.id, set1.Value.etag, req);
            Assert.IsTrue(set2.IsSuccess(), set2.Error?.MessageText);

            // Állapotváltozás NE legyen (idempotencia): enabled/method ugyanaz
            Assert.IsTrue(set2.Value.twoFactor.enabled);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP, set2.Value.twoFactor.method);

            // ETag viselkedés: vagy marad, vagy változik – ezt nem kényszerítjük,
            // csak azt ellenőrizzük, hogy a második hívás nem borította fel az állapotot.
            var read = await Sut.getEmailAuth(TestMain.ctx, acc.id, created.Value.id);
            Assert.IsTrue(read.IsSuccess());
            Assert.IsTrue(read.Value.twoFactor.enabled);
            Assert.AreEqual(IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP, read.Value.twoFactor.method);
        }

        [TestMethod]
        public async Task EmailAuth_SetTwoFactor_idempotent_for_SMS_with_same_phone()
        {
            var acc = await CreateUser("tfa_sms_idem");
            var created = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "tfa_sms_idem@example.com", "Good#Pass1234", Tfa());
            Assert.IsTrue(created.IsSuccess());

            var req = Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.SMS, phone: "+361234567");
            var set1 = await Sut.setTwoFactorOnEmailAuth(TestMain.ctx, acc.id, created.Value.id, created.Value.etag, req);
            Assert.IsTrue(set1.IsSuccess(), set1.Error?.MessageText);
            Assert.AreEqual("+361234567", set1.Value.twoFactor.phoneNumber);

            var set2 = await Sut.setTwoFactorOnEmailAuth(TestMain.ctx, acc.id, set1.Value.id, set1.Value.etag, req);
            Assert.IsTrue(set2.IsSuccess(), set2.Error?.MessageText);
            Assert.AreEqual("+361234567", set2.Value.twoFactor.phoneNumber);
        }


        // =====================================================================
        // 1) Globális e-mail egyediség (két külön account)
        // =====================================================================

        [TestMethod]
        public async Task EmailAuth_GlobalUniqueness_fail_same_email_on_different_accounts_case_insensitive()
        {
            var acc1 = await CreateUser("glob_u1");
            var acc2 = await CreateUser("glob_u2");

            var first = await Sut.createtEmailAuth(TestMain.ctx, acc1.id, "Global@X.com", MakeValidPassword(1), Tfa());
            Assert.IsTrue(first.IsSuccess(), first.Error?.MessageText);

            // Ugyanaz az e-mail más case-szel a MÁSODIK accounton -> elvárt: FAIL (globális egyediség)
            var second = await Sut.createtEmailAuth(TestMain.ctx, acc2.id, "global@x.com", MakeValidPassword(2), Tfa());
            Assert.IsTrue(second.IsFailed(), "Email should be globally unique across accounts (case-insensitive).");
            // opcionálisan:
            // Assert.AreEqual(ServiceKit.Net.Statuses.BadRequest, second.Error.Status);
            Assert.IsTrue(
                second.Error.MessageText.Contains("already", StringComparison.OrdinalIgnoreCase) ||
                second.Error.MessageText.Contains("exists", StringComparison.OrdinalIgnoreCase));
        }

        // Ha a rendszered még nem globálisan egyedi, a fenti teszt FAIL lesz — ez jelzi, hogy hiányzik az üzleti szabály.

        // =====================================================================
        // 2) Password history – N (12) korábbi jelszó tiltása
        //    - Nem lehet visszaállni a legutóbbi 12 közül bármelyikre
        //    - 13+ forgatás után a legelső újra használható (ha a policy így szól)
        // =====================================================================

        [TestMethod]
        public async Task PasswordHistory_disallow_reuse_of_last_12_allow_older_after_rotation()
        {
            var acc = await CreateUser("hist_user");

            // 0) Létrehozás kezdeti jelszóval
            var p0 = MakeValidPassword(0);
            var auth = await Sut.createtEmailAuth(TestMain.ctx, acc.id, "hist_user@example.com", p0, Tfa());
            Assert.IsTrue(auth.IsSuccess(), auth.Error?.MessageText);

            var current = auth.Value;

            // 1) Forgassunk végig 12 új jelszót (p1..p12)
            var passwords = Enumerable.Range(1, 12).Select(MakeValidPassword).ToArray();
            foreach (var p in passwords)
            {
                var chg = await Sut.changePasswordOnEmailAuth(TestMain.ctx, acc.id, current.id, current.etag, p);
                Assert.IsTrue(chg.IsSuccess(), chg.Error?.MessageText);
                current = chg.Value;
            }

            // 2) Próbáljuk visszaállítani bármelyik korábbi 12 közül – elvárt: FAIL
            foreach (var p in passwords)
            {
                var fail = await Sut.changePasswordOnEmailAuth(TestMain.ctx, acc.id, current.id, current.etag, p);
                Assert.IsTrue(fail.IsFailed(), $"Reusing recent password '{p}' should fail.");
                Assert.IsTrue(
                    fail.Error.MessageText.Contains("previously used", StringComparison.OrdinalIgnoreCase) ||
                    fail.Error.MessageText.Contains("history", StringComparison.OrdinalIgnoreCase) ||
                    fail.Error.MessageText.Contains("must be different", StringComparison.OrdinalIgnoreCase));
            }

            // 3) Most forgassunk MÉG EGYET (p13), hogy a legrégebbi (p1 vagy p0) kicsússzon a 12-es ablakból
            var p13 = MakeValidPassword(13);
            var ok13 = await Sut.changePasswordOnEmailAuth(TestMain.ctx, acc.id, current.id, current.etag, p13);
            Assert.IsTrue(ok13.IsSuccess(), ok13.Error?.MessageText);
            current = ok13.Value;

            // 4) Ha a rendszer a "12 utolsó" elvet követi, a legrégebbi régi jelszó (p0) most már ELVILEG használható
            // Megkíséreljük p0-t beállítani – elvárt: SUCCESS (ha a History_MaxCount=12 logika aktív)
            var reuseOldest = await Sut.changePasswordOnEmailAuth(TestMain.ctx, acc.id, current.id, current.etag, p0);
            // Ha a rendszered policy-ja tiltja a „bármikor előfordult” jelszót (nem csak az utolsó 12-t), itt FAIL lesz.
            // A következő két assert közül pontosíts a saját szabályod szerint:
            Assert.IsTrue(reuseOldest.IsSuccess(), reuseOldest.Error?.MessageText);
            // Assert.IsTrue(reuseOldest.IsFailed()); // <-- ezt válaszd, ha a teljes múltbeli reuse is tiltott.

            // 5) „aktuális jelszóra” váltás tilalma (current == p0 vagy p13 attól függően) – visszaellenőrzés
            var sameAsCurrent = await Sut.changePasswordOnEmailAuth(TestMain.ctx, acc.id, reuseOldest.Value.id, reuseOldest.Value.etag, p0);
            Assert.IsTrue(sameAsCurrent.IsFailed(), "New password must be different from the current password.");
        }

        private static string MakeValidPassword(int idx)
        {
            // Mindig érvényes a policyra (>=12, upper/lower/digit/spec, nincs whitespace, elég egyedi, nincs hosszú run)
            // pl.: "Aa#Pass1234_" + idx
            return $"Aa#Pass1234_{idx}";
        }
    }
}

