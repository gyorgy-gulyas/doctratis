using Core.Test.Mock;
using IAM.Identities.Identity;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using ServiceKit.Net;
using ServiceKit.Net.Communicators;
using System.Text.RegularExpressions;

namespace IAM.Identities.Tests
{
    [TestClass]
    public partial class LoginIF_v1_Email_Tests
    {
        private ILoginIF_v1 Sut => TestMain.ServiceProvider.GetRequiredService<ILoginIF_v1>();
        private IIdentityAdminIF_v1 Admin => TestMain.ServiceProvider.GetRequiredService<IIdentityAdminIF_v1>();

        [TestInitialize]
        public async Task Setup()
        {
            await TestMain.DeleteAllData();
        }

        // ---------- Helpers ----------

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

        private async Task<(IIdentityAdminIF_v1.AccountDTO acc, IIdentityAdminIF_v1.EmailAuthDTO auth)> CreateUserWithEmailAuthAsync(
            string accountName,
            string email,
            string initialPassword,
            IIdentityAdminIF_v1.TwoFactorConfigurationDTO twoFactor = null)
        {
            var accRes = await Admin.createAccount(TestMain.ctx, accountName, IIdentityAdminIF_v1.AccountTypes.User);
            Assert.IsTrue(accRes.IsSuccess(), accRes.Error?.MessageText);
            var acc = accRes.Value;

            var authRes = await Admin.createtEmailAuth(TestMain.ctx, acc.id, email, initialPassword, twoFactor ?? Tfa(enabled: false));
            Assert.IsTrue(authRes.IsSuccess(), authRes.Error?.MessageText);
            return (acc, authRes.Value);
        }

        private async Task ConfirmLastEmailAsync(string email)
        {
            var token = GetTokenFromLastConfirmationMailBody(email);
            Assert.IsFalse(string.IsNullOrWhiteSpace(token), "Failed to extract token from the email.");

            var confirm = await Sut.ConfirmEmail(TestMain.ctx, email, token);
            Assert.IsTrue(confirm.IsSuccess(), confirm.Error?.MessageText);
        }

        private string GetTokenFromLastConfirmationMailBody(string email)
        {
            var emailCommunicator = TestMain.ServiceProvider.GetRequiredService<IEmailCommunicator>() as Mock_EmailCommunicator;

            var toThisUser = emailCommunicator.sentMessages
                .Where(m => m.recipients != null && m.recipients.Any(r =>
                    string.Equals(r?.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase)));

            // latest email (if no timestamp, take the last one)
            var mail = toThisUser.LastOrDefault();
            Assert.IsNotNull(mail, $"No confirmation email found for '{email}'.");

            var match = Regex.Match(mail.body ?? string.Empty, "<([^>]+)>");
            Assert.IsTrue(match.Success, "Failed to extract token from email (no '<...>' pattern).");

            var token = match.Groups[1].Value.Trim();
            Assert.IsFalse(string.IsNullOrWhiteSpace(token), "Extracted token is empty.");

            return token;
        }

        private string GetTOTPFromLastMailBody(string email)
        {
            var emailCommunicator = TestMain.ServiceProvider.GetRequiredService<IEmailCommunicator>() as Mock_EmailCommunicator;

            var toThisUser = emailCommunicator
                .sentMessages
                .Where(m =>
                    m.recipients != null &&
                    m.recipients.Any(r => string.Equals(r?.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase)));

            var mail = toThisUser.LastOrDefault();
            Assert.IsNotNull(mail, $"No TOTP email found for '{email}'.");

            var match = Regex.Match(mail.body ?? string.Empty, "<([^>]+)>");
            Assert.IsTrue(match.Success, "Failed to extract TOTP from email (no '<...>' pattern).");

            var totp = match.Groups[1].Value.Trim();
            Assert.IsFalse(string.IsNullOrWhiteSpace(totp), "Extracted TOTP is empty.");

            return totp;
        }

        private string GetTOTPFromLastSMSText(string phoneNumber)
        {
            var smsCommunicator = TestMain.ServiceProvider.GetRequiredService<ISmsCommunicator>() as Mock_SmsCommunicator;

            var toThisPhone = smsCommunicator
                .sentMessages
                .Where(m =>
                    m.toPhoneNumber != null &&
                    string.Equals(m.toPhoneNumber.Trim(), phoneNumber.Trim(), StringComparison.OrdinalIgnoreCase));

            var mail = toThisPhone.LastOrDefault();
            Assert.IsNotNull(mail, $"No TOTP SMS found for '{phoneNumber}'.");

            var match = Regex.Match(mail.messageText ?? string.Empty, "<([^>]+)>");
            Assert.IsTrue(match.Success, "Failed to extract TOTP from SMS (no '<...>' pattern).");

            var totp = match.Groups[1].Value.Trim();
            Assert.IsFalse(string.IsNullOrWhiteSpace(totp), "Extracted TOTP is empty.");

            return totp;
        }


        // ---------- Tests ----------

        [TestMethod]
        public async Task Login_EmailPassword_success_without_2FA()
        {
            var (acc, auth) = await CreateUserWithEmailAuthAsync(
                accountName: "login_ok_no2fa",
                email: "user.login@example.com",
                initialPassword: "Good#Pass1234",
                twoFactor: Tfa(enabled: false));

            await ConfirmLastEmailAsync("user.login@example.com");
            var confirmed = await Admin.getEmailAuth(TestMain.ctx, acc.id, auth.id);

            var result = await Sut.LoginWithEmailPassword(TestMain.ctx, "user.login@example.com", "Good#Pass1234");
            Assert.IsTrue(result.IsSuccess(), result.Error?.MessageText);
            var login = result.Value;
            Assert.AreEqual(ILoginIF_v1.SignInResult.Ok, login.result);
        }

        [TestMethod]
        public async Task Login_EmailPassword_wrong_password_returns_InvalidUserNameOrPassword()
        {
            var (acc, auth) = await CreateUserWithEmailAuthAsync(
                accountName: "login_wrong_pw",
                email: "wrong.pw@example.com",
                initialPassword: "Good#Pass1234",
                twoFactor: Tfa(enabled: false));

            // confirm so we don’t fail because of “not confirmed”
            await ConfirmLastEmailAsync("wrong.pw@example.com");
            var confirmed = await Admin.getEmailAuth(TestMain.ctx, acc.id, auth.id);

            var result = await Sut.LoginWithEmailPassword(TestMain.ctx, "wrong.pw@example.com", "BadPassword!");
            Assert.IsTrue(result.IsSuccess(), "Even with wrong password, LoginWithEmailPassword must return a LoginResultDTO.");
            var login = result.Value;
            Assert.AreEqual(ILoginIF_v1.SignInResult.InvalidUserNameOrPassword, login.result, "Wrong password should return SignInResult.InvalidUserNameOrPassword.");
        }

        [TestMethod]
        public async Task Login_EmailPassword_unknown_email_prefers_InvalidUserNameOrPassword_or_NotFound()
        {
            var result = await Sut.LoginWithEmailPassword(TestMain.ctx, "no.such.user@example.com", "Whatever#123");
            var login = result.Value;
            Assert.AreEqual(ILoginIF_v1.SignInResult.InvalidUserNameOrPassword, result.Value.result, "Unknown email should return SignInResult.InvalidUserNameOrPassword (user enumeration protection).");
        }


        [TestMethod]
        public async Task Login_EmailPassword_fail_unknown_email()
        {
            // no confirm needed, account doesn’t exist
            var login = await Sut.LoginWithEmailPassword(TestMain.ctx, "no.such.user@example.com", "Whatever#123");
            Assert.AreEqual(ILoginIF_v1.SignInResult.InvalidUserNameOrPassword, login.Value.result);
        }

        [TestMethod]
        public async Task Login_EmailPassword_fail_when_email_auth_is_inactive()
        {
            var (acc, auth) = await CreateUserWithEmailAuthAsync(
                accountName: "login_inactive_auth",
                email: "inactive.auth@example.com",
                initialPassword: "Good#Pass1234",
                twoFactor: Tfa(enabled: false));

            // confirm first so it doesn’t fail because of “not confirmed”
            await ConfirmLastEmailAsync("inactive.auth@example.com");
            var confirmed = await Admin.getEmailAuth(TestMain.ctx, acc.id, auth.id);

            var deact = await Admin.setActiveForAuth(TestMain.ctx, acc.id, auth.id, confirmed.Value.etag, isActive: false);
            Assert.IsTrue(deact.IsSuccess(), deact.Error?.MessageText);
            Assert.IsFalse(deact.Value.isActive);

            var login = await Sut.LoginWithEmailPassword(TestMain.ctx, "inactive.auth@example.com", "Good#Pass1234");
            Assert.AreEqual(ILoginIF_v1.SignInResult.UserIsNotActive, login.Value.result);
        }


        [TestMethod]
        public async Task Login_EmailPassword_success_trim_and_case_insensitive_email()
        {
            var (acc, auth) = await CreateUserWithEmailAuthAsync(
                accountName: "login_trim_case",
                email: "CaseUser@Example.Com",
                initialPassword: "Good#Pass1234",
                twoFactor: Tfa(enabled: false));

            await ConfirmLastEmailAsync("CaseUser@Example.Com");
            var confirmed = await Admin.getEmailAuth(TestMain.ctx, acc.id, auth.id);

            var result = await Sut.LoginWithEmailPassword(TestMain.ctx, "   caseuser@example.com   ", "Good#Pass1234");
            Assert.IsTrue(result.IsSuccess(), result.Error?.MessageText);

            var login = result.Value;
            Assert.AreEqual(ILoginIF_v1.SignInResult.Ok, login.result, "Normalized email should return Ok.");
        }

        [TestMethod]
        public async Task Login_EmailPassword_requires_2FA_when_enabled_TOTP()
        {
            var (acc, auth) = await CreateUserWithEmailAuthAsync(
                accountName: "login_needs_2fa",
                email: "needs.2fa@example.com",
                initialPassword: "Good#Pass1234",
                twoFactor: Tfa(enabled: true, method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP));

            await ConfirmLastEmailAsync("needs.2fa@example.com");
            var confirmed = await Admin.getEmailAuth(TestMain.ctx, acc.id, auth.id);

            var result = await Sut.LoginWithEmailPassword(TestMain.ctx, "needs.2fa@example.com", "Good#Pass1234");
            var login = result.Value;
            Assert.AreEqual(ILoginIF_v1.SignInResult.Ok, login.result);
            Assert.IsTrue(login.requires2FA);
        }

        [TestMethod]
        public async Task Login_EmailPassword_inactive_email_auth_returns_UserIsNotActive()
        {
            var (acc, auth) = await CreateUserWithEmailAuthAsync(
                accountName: "login_inactive_auth",
                email: "inactive.auth@example.com",
                initialPassword: "Good#Pass1234",
                twoFactor: Tfa(enabled: false));

            await ConfirmLastEmailAsync("inactive.auth@example.com");
            var confirmed = await Admin.getEmailAuth(TestMain.ctx, acc.id, auth.id);

            // deactivate auth (no etag param in IF)
            var deact = await Admin.setActiveForAuth(TestMain.ctx, acc.id, auth.id, confirmed.Value.etag, isActive: false);
            Assert.IsTrue(deact.IsSuccess(), deact.Error?.MessageText);
            Assert.IsFalse(deact.Value.isActive);

            var result = await Sut.LoginWithEmailPassword(TestMain.ctx, "inactive.auth@example.com", "Good#Pass1234");
            Assert.AreEqual(ILoginIF_v1.SignInResult.UserIsNotActive, result.Value.result);
        }

        [TestMethod]
        public async Task Login_EmailPassword_with_Email2FA_success()
        {
            // 1) user + email auth + EMAIL 2FA target
            var (acc, auth) = await CreateUserWithEmailAuthAsync(
                accountName: "login_2fa_email_ok",
                email: "twofa.email@example.com",
                initialPassword: "Good#Pass1234",
                twoFactor: Tfa(enabled: true,
                               method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.Email,
                               email: "twofa.email@example.com"));   // 2FA code is sent here

            await ConfirmLastEmailAsync("twofa.email@example.com");

            // 2) Step 1: password check → requires2FA == true
            var step1 = await Sut.LoginWithEmailPassword(TestMain.ctx, "twofa.email@example.com", "Good#Pass1234");
            Assert.IsTrue(step1.IsSuccess(), step1.Error?.MessageText);
            Assert.AreEqual(ILoginIF_v1.SignInResult.Ok, step1.Value.result, "Step 1 must return Ok (password correct).");
            Assert.IsTrue(step1.Value.requires2FA);

            var code = GetTOTPFromLastMailBody("twofa.email@example.com");
            var clone_ctx = TestMain.ctx.CloneWithIdentity(acc.id, acc.data.Name, CallingContext.IdentityTypes.User);
            clone_ctx.Claims.Add("2faMethod", TwoFactorConfiguration.Methods.Email.ToString());

            var step2 = await Sut.Login2FA(clone_ctx, code);
            Assert.IsTrue(step2.IsSuccess(), step2.Error?.MessageText);
        }

        [TestMethod]
        public async Task Login_EmailPassword_with_Sms2FA_success()
        {
            // 1) user + email auth + SMS 2FA target
            var (acc, auth) = await CreateUserWithEmailAuthAsync(
                accountName: "login_2fa_email_ok",
                email: "twofa.email@example.com",
                initialPassword: "Good#Pass1234",
                twoFactor: Tfa(enabled: true,
                               method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.SMS,
                               email: "twofa.email@example.com",
                               phone: "+36193333333"));

            await ConfirmLastEmailAsync("twofa.email@example.com");

            // 2) Step 1: password check → requires2FA == true
            var step1 = await Sut.LoginWithEmailPassword(TestMain.ctx, "twofa.email@example.com", "Good#Pass1234");
            Assert.IsTrue(step1.IsSuccess(), step1.Error?.MessageText);
            Assert.AreEqual(ILoginIF_v1.SignInResult.Ok, step1.Value.result, "Step 1 must return Ok (password correct).");
            Assert.IsTrue(step1.Value.requires2FA);

            var code = GetTOTPFromLastSMSText("+36193333333");
            var clone_ctx = TestMain.ctx.CloneWithIdentity(acc.id, acc.data.Name, CallingContext.IdentityTypes.User);
            clone_ctx.Claims.Add("2faMethod", TwoFactorConfiguration.Methods.SMS.ToString());

            var step2 = await Sut.Login2FA(clone_ctx, code);
            Assert.IsTrue(step2.IsSuccess(), step2.Error?.MessageText);
        }


        [TestMethod]
        public async Task Login_EmailPassword_with_2FA_success()
        {
            // 1) user + email auth + TOTP 2FA target
            var (acc, auth) = await CreateUserWithEmailAuthAsync(
                accountName: "login_2fa_email_ok",
                email: "twofa.email@example.com",
                initialPassword: "Good#Pass1234",
                twoFactor: Tfa(enabled: true,
                               method: IIdentityAdminIF_v1.TwoFactorConfigurationDTO.Methods.TOTP,
                               email: "twofa.email@example.com",
                               phone: "+36193333333"));

            await ConfirmLastEmailAsync("twofa.email@example.com");

            // 2) Step 1: password check → requires2FA == true
            var step1 = await Sut.LoginWithEmailPassword(TestMain.ctx, "twofa.email@example.com", "Good#Pass1234");
            Assert.IsTrue(step1.IsSuccess(), step1.Error?.MessageText);
            Assert.AreEqual(ILoginIF_v1.SignInResult.Ok, step1.Value.result, "Step 1 must return Ok (password correct).");
            Assert.IsTrue(step1.Value.requires2FA);

            var accountRepository = TestMain.ServiceProvider.GetRequiredService<IAccountRepository>();
            var account = await accountRepository.getAccount(TestMain.ctx, acc.id);
            var totp = new Totp(Convert.FromBase64String(account.Value.accountSecret), step: 30);
            var code = totp.ComputeTotp();

            var clone_ctx = TestMain.ctx.CloneWithIdentity(acc.id, acc.data.Name, CallingContext.IdentityTypes.User);
            clone_ctx.Claims.Add("2faMethod", TwoFactorConfiguration.Methods.TOTP.ToString());

            var step2 = await Sut.Login2FA(clone_ctx, code);
            Assert.IsTrue(step2.IsSuccess(), step2.Error?.MessageText);
        }

        [TestMethod]
        public async Task ForgotPassword_sends_6_digit_code_via_email()
        {
            // Arrange
            var (acc, auth) = await CreateUserWithEmailAuthAsync(
                accountName: "forgot_pw_test",
                email: "forgot.user@example.com",
                initialPassword: "Initial#Pass1234");

            await ConfirmLastEmailAsync("forgot.user@example.com");

            // Act
            var forgot = await Sut.ForgotPassword(TestMain.ctx, "forgot.user@example.com");

            // Assert
            Assert.IsTrue(forgot.IsSuccess(), forgot.Error?.MessageText);

            var code = GetTOTPFromLastMailBody("forgot.user@example.com");
            Assert.AreEqual(6, code.Length, "Forgot password code must be 6 digits");
            Assert.IsTrue(int.TryParse(code, out _), "Forgot password code must be numeric");
        }

        [TestMethod]
        public async Task ResetPassword_with_valid_code_sets_new_password()
        {
            // Arrange
            var (acc, auth) = await CreateUserWithEmailAuthAsync(
                accountName: "reset_pw_test",
                email: "reset.user@example.com",
                initialPassword: "Initial#Pass1234");

            await ConfirmLastEmailAsync("reset.user@example.com");

            // Trigger forgot password flow
            var forgot = await Sut.ForgotPassword(TestMain.ctx, "reset.user@example.com");
            Assert.IsTrue(forgot.IsSuccess(), forgot.Error?.MessageText);

            var code = GetTOTPFromLastMailBody("reset.user@example.com");
            var newPassword = "New#SecurePass123";

            // Act
            var reset = await Sut.ResetPassword(TestMain.ctx, "reset.user@example.com", code, newPassword);

            // Assert
            Assert.IsTrue(reset.IsSuccess(), reset.Error?.MessageText);

            // Login with new password
            var login = await Sut.LoginWithEmailPassword(TestMain.ctx, "reset.user@example.com", newPassword);
            Assert.AreEqual(ILoginIF_v1.SignInResult.Ok, login.Value.result, "Login should succeed with new password");
        }

        [TestMethod]
        public async Task ResetPassword_fails_for_unknown_email()
        {
            var result = await Sut.ResetPassword(TestMain.ctx, "doesnotexist@example.com", "123456", "NewPassword#1");
            Assert.IsFalse(result.IsSuccess(), "ResetPassword should fail for unknown email.");
            Assert.AreEqual(Statuses.NotFound, result.Error?.Status);
        }

        [TestMethod]
        public async Task ResetPassword_fails_for_invalid_token()
        {
            var (acc, auth) = await CreateUserWithEmailAuthAsync(
                accountName: "reset_fail_token",
                email: "fail.token@example.com",
                initialPassword: "Initial#1234");

            await ConfirmLastEmailAsync("fail.token@example.com");

            var forgotResult = await Sut.ForgotPassword(TestMain.ctx, "fail.token@example.com");
            Assert.IsTrue(forgotResult.IsSuccess());

            var resetResult = await Sut.ResetPassword(TestMain.ctx, "fail.token@example.com", "badtoken", "NewPassword#1234");
            Assert.IsFalse(resetResult.IsSuccess(), "Reset should fail with wrong token.");
            Assert.AreEqual(Statuses.BadRequest, resetResult.Error?.Status);
        }
    }
}
