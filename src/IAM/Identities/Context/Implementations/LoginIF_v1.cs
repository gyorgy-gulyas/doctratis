using ServiceKit.Net;

namespace IAM.Identities.Service.Implementations
{
    public class LoginIF_v1 : ILoginIF_v1
    {
        private readonly ILoginService _loginService;

        public LoginIF_v1(ILoginService loginService)
        {
            _loginService = loginService;
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
            var change = await _loginService.ChangePassword(ctx, email, oldPassword, newPassword).ConfigureAwait(false);
            if (change.IsFailed())
                return new(change.Error);

            return Response.Success();
        }

        async Task<Response> ILoginIF_v1.ForgotPassword(CallingContext ctx, string email)
        {
            var forgot = await _loginService.ForgotPassword(ctx, email).ConfigureAwait(false);
            if (forgot.IsFailed())
                return new(forgot.Error);

            return Response.Success();
        }

        async Task<Response> ILoginIF_v1.ResetPassword(CallingContext ctx, string email, string token, string newPassword)
        {
            var forgot = await _loginService.ResetPassword(ctx, email, token, newPassword).ConfigureAwait(false);
            if (forgot.IsFailed())
                return new(forgot.Error);

            return Response.Success();
        }

        async Task<Response> ILoginIF_v1.ConfirmEmail(CallingContext ctx, string email, string token)
        {
            var confirm = await _loginService.ConfirmEmail(ctx, email, token).ConfigureAwait(false);
            if (confirm.IsFailed())
                return new(confirm.Error);

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
