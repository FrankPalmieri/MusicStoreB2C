using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using MusicStoreB2C.App_Start;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace MusicStoreB2C.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        public AccountController(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AccountController>();
        }
        // GET: /Account/Login
        [HttpGet]
        public async Task SignUp()
        {
            _logger.LogInformation("AccountController.SignUp()");
            if (HttpContext.User == null || !HttpContext.User.Identity.IsAuthenticated)
            {
                var authenticationProperties = new AuthenticationProperties { RedirectUri = "/" };
                await HttpContext.Authentication.ChallengeAsync(ActiveDirB2C.SignUpPolicyId.ToLower(), authenticationProperties);
            }
        }

        // GET: /Account/Login
        [HttpGet]
        public async Task SignIn()
        {
            _logger.LogInformation("AccountController.SignIn()");
            if (HttpContext.User == null || !HttpContext.User.Identity.IsAuthenticated)
            {
                var authenticationProperties = new AuthenticationProperties { RedirectUri = "/" };
                await HttpContext.Authentication.ChallengeAsync(ActiveDirB2C.SignInPolicyId.ToLower(), authenticationProperties);
            }
        }

        // GET: /Account/Login
        [HttpGet]
        public async Task SignUpOrIn()
        {
            _logger.LogInformation("AccountController.SignUpOrIn()");
            if (HttpContext.User == null || !HttpContext.User.Identity.IsAuthenticated)
            {
                var authenticationProperties = new AuthenticationProperties { RedirectUri = "/" };
                await HttpContext.Authentication.ChallengeAsync(ActiveDirB2C.SignUpOrInPolicyId.ToLower(), authenticationProperties);
            }
        }

        // GET: /Account/LogOff
        [HttpGet]
        public async Task LogOff()
        {
            _logger.LogInformation("AccountController.LogOff()");
            if (HttpContext.User != null && HttpContext.User.Identity.IsAuthenticated)
            {
                // The user needs to be signed out based on the policy they were signed in
                // under - for some configuration, it is apparently under the string above, for this sample 
                // the key is tfp for some reason try to find the tfp policy id claim (default)  
                // https://github.com/Azure-Samples/active-directory-dotnet-webapp-openidconnect-aspnetcore-b2c/pull/8
                //
                var scheme = (HttpContext.User.FindFirst("tfp"))?.Value;
                
                // fall back to legacy acr policy id claim  
                if (string.IsNullOrEmpty(scheme))
                    scheme = (HttpContext.User.FindFirst("http://schemas.microsoft.com/claims/authnclassreference"))?.Value;

                await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.Authentication.SignOutAsync(scheme.ToLower(), new AuthenticationProperties { RedirectUri = "/" });
            }
        }

        [HttpGet]
        public async Task Profile()
        {
            _logger.LogInformation("AccountController.Profile()");
            if (HttpContext.User == null || HttpContext.User.Identity.IsAuthenticated)
            {
                // the last parameter seems to be a bug 
                // see https://github.com/Azure-Samples/active-directory-dotnet-webapp-openidconnect-aspnetcore-b2c/issues/2
                // or here https://joonasw.net/view/azure-ad-b2c-with-aspnet-core

                var authenticationProperties = new AuthenticationProperties { RedirectUri = "/" };
                await HttpContext.Authentication.ChallengeAsync(ActiveDirB2C.ProfilePolicyId.ToLower(), authenticationProperties, 
                    Microsoft.AspNetCore.Http.Features.Authentication.ChallengeBehavior.Unauthorized);
            }
        }

        [HttpGet]
        public async Task PasswordReset()
        {
            _logger.LogInformation("AccountController.PasswordReset()");
            if (HttpContext.User == null || HttpContext.User.Identity.IsAuthenticated)
            {
                var authenticationProperties = new AuthenticationProperties { RedirectUri = "/" };
                await HttpContext.Authentication.ChallengeAsync(ActiveDirB2C.ResetPasswordPolicyId.ToLower(), authenticationProperties,
                    Microsoft.AspNetCore.Http.Features.Authentication.ChallengeBehavior.Unauthorized);
            }
        }

        public IActionResult AccessDenied()
        {
            return View("~/Views/Shared/AccessDenied.cshtml");
        }

        //TODO: I think this needs to be wired up in startup

        [HttpGet] 
        public async Task EndSession()
        {
            _logger.LogInformation("AccountController.EndSession()");
            // If AAD sends a single sign-out message to the app, end the user's session, but don't redirect to AAD for sign out. 
            await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); 
        }
    }
}
