using EventsService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Added for ILogger

namespace EventsService.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ILogger<AccountController> _logger; // Added logger

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<Role> roleManager,
            ILogger<AccountController> logger) // Add logger
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger; // Assign logger
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
                    var roles = await _userManager.GetRolesAsync(user);
                    _logger.LogInformation($"User {user.UserName} (Email: {email}) attempting to log in. Roles: [{string.Join(", ", roles)}]");

                    var result = await _signInManager.PasswordSignInAsync(user.UserName, password, rememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"User {user.UserName} logged in successfully.");
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else // Login failed for known user
                    {
                        _logger.LogWarning($"Failed login attempt for user {user.UserName}. Result: {result}");
                    }
                }
                else // User not found
                {
                    _logger.LogWarning($"Login attempt for unknown email: {email}");
                }

                // If we reach here, login was unsuccessful (either user not found or password mismatch)
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
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
