using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceKit.Net;

namespace Core.Identities.Service.Controllers
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
            CallingContext ctx = CallingContext.PoolFromHttpContext(HttpContext, _logger);
            var clone = ctx.CloneWithIdentity("KAU", "KAU", CallingContext.IdentityTypes.Service);

            var response = _loginService.KAUCallback(clone, code, state);
            if(response.)


            return Redirect($"{url}?accessToken={result.Value.tokens.AccessToken}&refreshToken={result.Value.tokens.RefreshToken}");
        }
    }
}