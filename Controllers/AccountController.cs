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
                    else // This 'else' corresponds to 'if (result.Succeeded)'
                    {
                        // If PasswordSignInAsync failed, check 'result' for specific failure reasons.
                        if (result.IsLockedOut)
                        {
                            // _logger.LogWarning($"User account locked out for {email}."); // Example logging
                            ModelState.AddModelError(string.Empty, "User account locked out.");
                        }
                        else if (result.IsNotAllowed)
                        {
                            // _logger.LogWarning($"User {email} is not allowed to sign in."); // Example logging
                            ModelState.AddModelError(string.Empty, "User is not allowed to sign in. Email may not be confirmed.");
                        }
                        else if (result.RequiresTwoFactor)
                        {
                            // return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, rememberMe }); // Example 2FA
                            ModelState.AddModelError(string.Empty, "Two-factor authentication is required.");
                        }
                        else
                        {
                            // General failure (e.g. wrong password for the found user)
                            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        }
                    }
                }
                else // This 'else' corresponds to 'if (user != null)'
                {
                    // User was not found by email.
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
                // If we've reached here, it means either user was null, or sign-in failed.
                // In either case, ModelState will have an error, and we should return the View.
                // return View(model); // If using a ViewModel
                return View(); // Current structure, returning View for any login failure not resulting in redirect
            }

            // If ModelState was initially invalid (e.g., email or password format wrong, if validated by attributes on a ViewModel)
            // This is the fallback if the initial ModelState.IsValid is false.
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
