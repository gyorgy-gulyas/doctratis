using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ServiceKit.Net;

namespace IAM.Identities.Service.Controllers
{
    [Route("api/kau")]
    [ApiController]
    [AllowAnonymous]
    public class KAUCallback_RestController : ControllerBase
    {
        private readonly ILogger<LoginIF_v1_RestController> _logger;
        private readonly ILoginService _loginService;
        private readonly IHostEnvironment _hostEnvironment;

        public KAUCallback_RestController(ILoginService loginService, ILogger<LoginIF_v1_RestController> logger, IHostEnvironment hostEnvironment)
        {
            _loginService = loginService;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
        {
            var ctx = CallingContext.PoolFromHttpContext(HttpContext, _logger)
                        .CloneWithIdentity("KAU", "KAU", CallingContext.IdentityTypes.Service);

            var response = await _loginService.KAUCallback(ctx, code, state);
            if (response.IsFailed())
                return BadRequest(response.Error.MessageText);

            var login = response.Value.result;
            var queryParams = _BuildQueryParams(login);

            var redirectUrl = QueryHelpers.AddQueryString(response.Value.returnUrl, queryParams);
            return Redirect(redirectUrl);

            static Dictionary<string, string> _BuildQueryParams(ILoginIF_v1.LoginResultDTO login)
            {
                var qp = new Dictionary<string, string>
                {
                    ["sign_in_result"] = login.result.ToString()
                };

                if (login.tokens != null)
                {
                    qp["accessToken"] = login.tokens.AccessToken;
                    qp["refreshToken"] = login.tokens.RefreshToken;
                    qp["requires_2fa"] = login.requires2FA.ToString();
                    qp["accessTokenExpiresAt"] = login.tokens.AccessTokenExpiresAt.ToString("yyyyMMddHHmmss");
                    qp["refreshTokenExpiresAt"] = login.tokens.RefreshTokenExpiresAt.ToString("yyyyMMddHHmmss");
                }

                return qp;
            }
        }
    }
}