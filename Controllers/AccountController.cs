using EventsService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EventsService.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<Role> _roleManager;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<Role> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword) // Replace with a ViewModel later
        {
            // Basic validation for now, proper ViewModel and validation attributes should be used
            if (password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return View();
            }

            if (ModelState.IsValid)
            {
                var user = new User { UserName = email, Email = email };
                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    // For now, sign in the user directly. Email confirmation could be added.
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View();
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null) // Replace with a ViewModel later
        {
            ViewData["ReturnUrl"] = returnUrl;
            // It's good practice to create a ViewModel for login (e.g., LoginViewModel)
            // and pass it back to the View in case of errors.
            // For now, we'll stick to the current structure but acknowledge this.
            // var model = new LoginViewModel { Email = email, RememberMe = rememberMe }; // If we had a ViewModel

            if (ModelState.IsValid) // This check remains, though its utility is limited without a ViewModel
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    // Now try to sign in with the found user's UserName
                    var result = await _signInManager.PasswordSignInAsync(user.UserName, password, rememberMe, lockoutOnFailure: false);
                    // Alternatively, if supported and preferred (usually PasswordSignInAsync takes username string):
                    // var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        // _logger.LogInformation($"User {user.UserName} logged in."); // Example logging
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }
                // If user is null, result.Succeeded will be false by virtue of not attempting PasswordSignInAsync or it failing.
                // If user was found but PasswordSignInAsync failed, result will reflect that.
                // So, the following handles both user not found and password incorrect, and other states.

                if (result != null) // result would be null if user was null and we didn't proceed to PasswordSignInAsync
                {
                    if (result.IsLockedOut)
                    {
                        // _logger.LogWarning($"User account locked out for email {email}."); // Example logging
                        ModelState.AddModelError(string.Empty, "Учетная запись заблокирована.");
                    }
                    else if (result.IsNotAllowed)
                    {
                        // _logger.LogWarning($"User not allowed to sign in for email {email}. (EmailConfirmed? {user?.EmailConfirmed})"); // Example logging
                        ModelState.AddModelError(string.Empty, "Вход не разрешен. Возможно, требуется подтверждение email?");
                        // Note: Our current setup has EmailConfirmed = true for new users and SignIn.RequireConfirmedAccount = false,
                        // so this specific IsNotAllowed case due to email confirmation is unlikely unless settings change.
                    }
                    else if (result.RequiresTwoFactor)
                    {
                        // _logger.LogWarning($"Two-factor authentication required for email {email}."); // Example logging
                        // return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe }); // If 2FA is implemented
                        ModelState.AddModelError(string.Empty, "Требуется двухфакторная аутентификация.");
                    }
                    else // This will catch general failures like wrong password
                    {
                        // _logger.LogWarning($"Invalid login attempt for email {email}."); // Example logging
                        ModelState.AddModelError(string.Empty, "Неверная попытка входа (неправильный email или пароль).");
                    }
                }
                else // This case handles if user was null (user not found by email)
                {
                     ModelState.AddModelError(string.Empty, "Неверная попытка входа (пользователь не найден).");
                }
                // return View(model); // If using a ViewModel
                return View(); // Current structure
            }

            // return View(model); // If using a ViewModel
            return View(); // Current structure
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
