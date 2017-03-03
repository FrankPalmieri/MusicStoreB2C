using MusicStoreB2C;
using MusicStoreB2C.Models;
using MusicStoreB2C.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Dynamic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MusicStoreB2C.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private readonly ILogger<ManageController> _logger;
        public ManageController(MusicStoreContext dbContext, ILogger<ManageController> logger
            // ,UserManager<ApplicationUser> userManager,
            // SignInManager<ApplicationUser> signInManager
            )
        {
            DbContext = dbContext;
            _logger = logger;
            // UserManager = userManager;
            // SignInManager = signInManager;
        }

        public UserManager<ApplicationUser> UserManager { get; }

        public SignInManager<ApplicationUser> SignInManager { get; }

        public MusicStoreContext DbContext { get; }
        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index([FromServices] MusicStoreContext dbContext, ManageMessageId? message = null)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            var user = await GetCurrentUserAsync();

            var orderHistory = OrderHistory.GetOrderHistory(dbContext, user);
            dynamic userProps = await Startup.adB2C.GetUserProperties(user).Result;
            string mobile = userProps.mobile;

            var model = new IndexViewModel
            {
                HasPassword = false, // await UserManager.HasPasswordAsync(user),
                HasOrders = await orderHistory.HasOrdersAsync(),
                PhoneNumber = mobile, // await UserManager.GetPhoneNumberAsync(user),
                TwoFactor = false, // await UserManager.GetTwoFactorEnabledAsync(user),
                Logins = null, // await UserManager.GetLoginsAsync(user),
                BrowserRemembered = false, //= await SignInManager.IsTwoFactorClientRememberedAsync(user)
                ThumbnailPhoto = await Startup.adB2C.GetUserThumbnail(user),
                UserProps = userProps
            };

            return View(model);
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message = ManageMessageId.Error;
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = new IdentityResult(); // await UserManager.RemoveLoginAsync(user, loginProvider, providerKey);
                if (result.Succeeded)
                {
                    // await SignInManager.SignInAsync(user, isPersistent: false);
                    message = ManageMessageId.RemoveLoginSuccess;
                }
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // GET: /Account/AddPhoneNumber
        public IActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Account/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await GetCurrentUserAsync();
            // Generate the token and send it
            var code = ""; // await UserManager.GenerateChangePhoneNumberTokenAsync(user, model.Number);
            await MessageServices.SendSmsAsync(model.Number, "Your security code is: " + code);
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactorAuthentication()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                // await UserManager.SetTwoFactorEnabledAsync(user, true);
                // TODO: flow remember me somehow?
                // await SignInManager.SignInAsync(user, isPersistent: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactorAuthentication()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                // await UserManager.SetTwoFactorEnabledAsync(user, false);
                // await SignInManager.SignInAsync(user, isPersistent: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Account/VerifyPhoneNumber
        public async Task<IActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            // This code allows you exercise the flow without actually sending codes
            // For production use please register a SMS provider in IdentityConfig and generate a code here.
#if DEMO
            ViewBag.Code = ""; // await UserManager.GenerateChangePhoneNumberTokenAsync(await GetCurrentUserAsync(), phoneNumber);
#endif
            await Task.Delay(1);
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Account/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = new IdentityResult(); // await UserManager.ChangePhoneNumberAsync(user, model.PhoneNumber, model.Code);
                if (result.Succeeded)
                {
                    // await SignInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
                }
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // GET: /Account/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePhoneNumber()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = new IdentityResult(); //await UserManager.SetPhoneNumberAsync(user, null);
                if (result.Succeeded)
                {
                    // await SignInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction(nameof(Index), new { Message = ManageMessageId.RemovePhoneSuccess });
                }
            }
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
        }

        //
        // GET: /Manage/ChangePassword
        public IActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = new IdentityResult(); //await UserManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    // await SignInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
                }
                AddErrors(result);
                return View(model);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.Error });
        }

        //
        // GET: /Manage/SetPassword
        public IActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                var result = new IdentityResult(); //await UserManager.AddPasswordAsync(user, model.NewPassword);
                if (result.Succeeded)
                {
                    // await SignInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
                return View(model);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.Error });
        }

        //
        // POST: /Manage/RememberBrowser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RememberBrowser()
        {
            var user = await GetCurrentUserAsync();
            if (user != null)
            {
                // await SignInManager.RememberTwoFactorClientAsync(user);
                // await SignInManager.SignInAsync(user, isPersistent: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/ForgetBrowser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgetBrowser()
        {
            await SignInManager.ForgetTwoFactorClientAsync();
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Account/Manage
        public async Task<IActionResult> ManageLogins(ManageMessageId? message = null)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.AddLoginSuccess ? "The external login was added."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            // var userLogins = await UserManager.GetLoginsAsync(user);
            // var otherLogins = SignInManager.GetExternalAuthenticationSchemes().Where(auth => userLogins.All(ul => auth.AuthenticationScheme != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = false; // user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                // CurrentLogins = userLogins,
                // OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Action("LinkLoginCallback", "Manage");
            var properties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, UserManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }

            var loginInfo = (ExternalLoginInfo)null; // await SignInManager.GetExternalLoginInfoAsync(await UserManager.GetUserIdAsync(user));
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }

            var result = (IdentityResult)null; // await UserManager.AddLoginAsync(user, loginInfo);
            var message = result.Succeeded ? ManageMessageId.AddLoginSuccess : ManageMessageId.Error;
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // GET: /Account/ViewOrders
        public async Task<ActionResult> ViewOrders()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var orderHistory = OrderHistory.GetOrderHistory(DbContext, user);

            var viewModel = new ViewOrdersViewModel
            {
                Orders = await orderHistory.GetOrders(),
                OrderTotal = await orderHistory.GetOrdersTotal(),
            };
            return View(viewModel);
        }

        //
        // GET: /Account/ViewOrderDetails
        public async Task<ActionResult> ViewOrderDetails(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var orderHistory = OrderHistory.GetOrderHistory(DbContext, user);

            var viewModel = new ViewOrderDetailsViewModel
            {
                OrderId = id,
                OrderDetails = await orderHistory.GetOrderDetails(id),
                OrderTotal = await orderHistory.GetOrderTotal(id),
            };
            return View(viewModel);
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            AddLoginSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

        private Task<ClaimsPrincipal> GetCurrentUserAsync()
        {
            //            return UserManager.GetUserAsync(HttpContext.User);
            return Task.FromResult(User);
        }

        #endregion
    }
}