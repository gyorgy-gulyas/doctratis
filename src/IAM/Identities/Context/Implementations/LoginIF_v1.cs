using OtpNet;
using ServiceKit.Net;
using Twilio.TwiML.Voice;

namespace IAM.Identities.Service.Implementations
{
    public class LoginIF_v1 : ILoginIF_v1
    {
        private readonly ILoginService _loginService;
        private readonly IAccountService _accountService;
        private readonly IAccountAuthService _accountAuthService;

        public LoginIF_v1(
            ILoginService loginService,
            IAccountService accountService,
            IAccountAuthService accountAuthService)
        {
            _loginService = loginService;
            _accountAuthService = accountAuthService;
            _accountService = accountService;
        }

        Task<Response<ILoginIF_v1.LoginResultDTO>> ILoginIF_v1.LoginWithEmailPassword(CallingContext ctx, string email, string password)
        {
            return _loginService.LoginWithEmailPassword(ctx, email, password);
        }

        public Task<Response<ILoginIF_v1.LoginResultDTO>> LoginWithAD(CallingContext ctx, string username, string password)
        {
            return _loginService.LoginWithAD(ctx, username, password);
        }

        Task<Response<ILoginIF_v1.TokensDTO>> ILoginIF_v1.Login2FA(CallingContext ctx, string totp)
        {
            return _loginService.Login2FA(ctx, totp);
        }

        async Task<Response> ILoginIF_v1.ChangePassword(CallingContext ctx, string email, string oldPassword, string newPassword)
        {
            var find = await _accountService.findAccountByEmailAuth(ctx, email).ConfigureAwait(false);
            if (find.IsFailed())
                return new(find.Error);

            var result = await _accountAuthService.changePassword(ctx,
                accountId: find.Value.account.id,
                authId: find.Value.auth.id, 
                etag: find.Value.auth.etag, 
                oldPassword,
                newPassword).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            return Response.Success();
        }

        async Task<Response> ILoginIF_v1.ForgottPassword(CallingContext ctx, string email, string url)
        {
            var find = await _accountService.findAccountByEmailAuth(ctx, email).ConfigureAwait(false);
            if (find.IsFailed())
                return new(find.Error);

            var result = await _accountAuthService.ForgottPassword(ctx,
                accountId: find.Value.account.id,
                email,
                url).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            return Response.Success();
        }

        async Task<Response> ILoginIF_v1.ResetPassword(CallingContext ctx, string token, string url)
        {
            var result = await _accountAuthService.ResetPassword(ctx, token, url).ConfigureAwait(false);
            if (result.IsFailed())
                return new(result.Error);

            return Response.Success();
        }

        Task<Response<ILoginIF_v1.TokensDTO>> ILoginIF_v1.RefreshTokens(CallingContext ctx, string refreshToken)
        {
            return _loginService.RefreshTokens(ctx, refreshToken);
        }

        Task<Response<ILoginIF_v1.LoginResultDTO>> ILoginIF_v1.LoginWithAD(CallingContext ctx, string username, string password)
        {
            return _loginService.LoginWithAD(ctx, username, password);
        }

        Task<Response<string>> ILoginIF_v1.GetKAULoginURL(CallingContext ctx, string redirectUrl)
        {
            throw new NotImplementedException();
        }
    }
}
