using EventsService.Data;
using EventsService.Models;
using EventsService.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Added for SelectList and MultiSelectList
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventsService.Controllers
{
    [Authorize(Roles = "Admin")] // Base authorization for the controller
    public class UsersController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ApplicationDbContext _context; // Optional, but good to have if complex queries are needed

        public UsersController(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = roles
                });
            }
            // Order by UserName for consistent display
            return View(userViewModels.OrderBy(u => u.UserName).ToList());
        }

        // GET: Users/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateUserViewModel();
            await PopulateAvailableRoles(viewModel);
            return View(viewModel);
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Ensure the current user is authorized to assign the selected role
                var selectedRole = await _roleManager.FindByIdAsync(model.RoleId.ToString());
                if (selectedRole == null)
                {
                    ModelState.AddModelError("RoleId", "Выбранная роль не найдена.");
                    await PopulateAvailableRoles(model);
                    return View(model);
                }

                // Note: The controller itself is [Authorize(Roles="Admin")].
                // If Managers were allowed to access this controller's Create action (e.g. if Auth was Admin,Manager)
                // then IsCurrentUserAuthorizedToAssignRole would be essential.
                // With current [Authorize(Roles="Admin")] on controller, Admin can assign any role,
                // so this check is somewhat redundant for now but good for future flexibility.
                if (!await IsCurrentUserAuthorizedToAssignRole(selectedRole.Name ?? ""))
                {
                    ModelState.AddModelError(string.Empty, "У вас нет прав назначать эту роль.");
                    await PopulateAvailableRoles(model);
                    return View(model);
                }

                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true, // EmailConfirmed = true for simplicity for now
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Position = model.Position
                };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    var roleAddResult = await _userManager.AddToRoleAsync(user, selectedRole.Name ?? ""); // Ensure selectedRole.Name is not null
                    if (roleAddResult.Succeeded)
                    {
                        // TempData["SuccessMessage"] = $"Пользователь {user.Email} успешно создан с ролью {selectedRole.Name}.";
                        return RedirectToAction(nameof(Index));
                    }
                    foreach (var error in roleAddResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    // If adding role failed, user is created but without role. This is a partial success.
                    // Consider deleting the user or providing a way to assign role later.
                    // For now, fall through to show errors.
                    // It might be better to delete the user if role assignment fails:
                    // await _userManager.DeleteAsync(user);
                    // ModelState.AddModelError(string.Empty, "Failed to assign role. User creation rolled back.");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            await PopulateAvailableRoles(model);
            return View(model);
        }

        // Helper method to populate available roles based on current user's permissions
        private async Task PopulateAvailableRoles(CreateUserViewModel model)
        {
            List<Role> rolesToShow;
            // Since the controller is [Authorize(Roles = "Admin")], only Admin can access actions here.
            // The logic for Manager is currently not reachable but kept for potential future adjustments.
            if (User.IsInRole("Admin"))
            {
                // Admin sees all roles.
                rolesToShow = await _roleManager.Roles.ToListAsync();
            }
            else if (User.IsInRole("Manager"))
            {
                // Manager would see a restricted list (e.g., SalesRepresentative, Client)
                // This part is currently not active due to controller-level authorization.
                rolesToShow = await _roleManager.Roles
                    .Where(r => r.Name == "SalesRepresentative" || r.Name == "Client")
                    .ToListAsync();
            }
            else
            {
                rolesToShow = new List<Role>(); // Should not happen if controller is locked to Admin
            }
            model.AvailableRoles = new SelectList(rolesToShow.OrderBy(r => r.Name), "Id", "Name", model.RoleId);
        }

        // Helper method to check if current user can assign a specific role
        private async Task<bool> IsCurrentUserAuthorizedToAssignRole(string roleName)
        {
            // Since controller is [Authorize(Roles = "Admin")], current user is always Admin.
            // Admin can assign any role.
            if (User.IsInRole("Admin"))
            {
                return true;
            }
            // The following logic for Manager is currently not reachable.
            // Kept for future reference if controller authorization changes.
            if (User.IsInRole("Manager"))
            {
                return roleName == "SalesRepresentative" || roleName == "Client";
            }
            return false; // Default deny
        }

        // GET: Users/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id) // Assuming Id is int, consistent with IdentityUser<int>
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Position = user.Position ?? string.Empty,
                CurrentRoles = userRoles.ToList() // For display
            };

            await PopulateRolesForEdit(model, user);

            return View(model);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id.ToString());
                if (user == null)
                {
                    return NotFound();
                }

                user.UserName = model.UserName;
                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Position = model.Position;
                // Consider EmailConfirmed status if it's part of your workflow

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    AddIdentityErrors(updateResult);
                    await PopulateRolesForEdit(model, user); // Re-populate for view
                    return View(model);
                }

                // Role management
                var currentRoles = await _userManager.GetRolesAsync(user);
                var selectedRoleObjects = new List<Role>();
                foreach(var roleId in model.SelectedRoleIds ?? new List<int>())
                {
                    var role = await _roleManager.FindByIdAsync(roleId.ToString());
                    if (role != null && role.Name != null) selectedRoleObjects.Add(role); // Ensure role.Name is not null
                }
                var selectedRoleNames = selectedRoleObjects.Select(r => r.Name!).ToList(); // Use null-forgiving operator for r.Name

                // Check authorization for all selected roles
                foreach (var roleName in selectedRoleNames)
                {
                    if (string.IsNullOrEmpty(roleName)) continue;
                    if (!await IsCurrentUserAuthorizedToAssignRole(roleName)) // IsCurrentUserAuthorizedToAssignRole is from Create logic
                    {
                        ModelState.AddModelError(string.Empty, $"У вас нет прав назначать роль '{roleName}'.");
                        await PopulateRolesForEdit(model, user);
                        return View(model);
                    }
                }
                // More granular check: prevent manager from removing Admin/Manager roles if they didn't assign them
                if (User.IsInRole("Manager"))
                {
                    var rolesUserHadThatManagerCannotTouch = currentRoles.Where(r => r == "Admin" || r == "Manager").ToList();
                    var rolesUserWillHaveThatManagerCannotTouch = selectedRoleNames.Where(r => r == "Admin" || r == "Manager").ToList();
                    if (rolesUserHadThatManagerCannotTouch.Count() != rolesUserWillHaveThatManagerCannotTouch.Count() ||
                        !rolesUserHadThatManagerCannotTouch.All(r => rolesUserWillHaveThatManagerCannotTouch.Contains(r)))
                        {
                            ModelState.AddModelError(string.Empty, "Менеджер не может изменять роли Администратора или другого Менеджера.");
                            await PopulateRolesForEdit(model, user);
                            return View(model);
                        }
                }

                var rolesToAdd = selectedRoleNames.Except(currentRoles).ToList();
                var rolesToRemove = currentRoles.Except(selectedRoleNames).ToList();

                if (rolesToAdd.Any())
                {
                    var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                    if (!addResult.Succeeded) AddIdentityErrors(addResult);
                }

                if (rolesToRemove.Any())
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    if (!removeResult.Succeeded) AddIdentityErrors(removeResult);
                }

                if (!ModelState.IsValid) // Check if AddIdentityErrors added anything
                {
                     await PopulateRolesForEdit(model, user); // Re-populate for view
                     return View(model);
                }

                // TempData["SuccessMessage"] = $"Данные пользователя {user.UserName} успешно обновлены.";
                return RedirectToAction(nameof(Index));
            }

            // If ModelState is invalid, re-populate roles. Need user object for CurrentRoles.
            var userForRepopulate = await _userManager.FindByIdAsync(model.Id.ToString());
            if (userForRepopulate != null)
            {
                await PopulateRolesForEdit(model, userForRepopulate);
            }
            else
            {
                // Fallback if user somehow cannot be found, though Id should be valid from hidden field
                model.AllRoles = new MultiSelectList(new List<Role>(), "Id", "Name");
                model.CurrentRoles = new List<string>();
            }

            return View(model);
        }

        // Helper to populate roles for the EditUserViewModel
        private async Task PopulateRolesForEdit(EditUserViewModel model, User user)
        {
            var allSystemRoles = await _roleManager.Roles.ToListAsync();
            List<Role> assignableRoles;

            // Since controller is [Authorize(Roles = "Admin")], current user is always Admin.
            if (User.IsInRole("Admin"))
            {
                assignableRoles = allSystemRoles;
            }
            // The following Manager logic is currently not reachable with controller-level Admin authorization
            // but is kept for potential future role expansion for this controller.
            else if (User.IsInRole("Manager"))
            {
                // Manager can only see/assign SalesRepresentative and Client roles
                assignableRoles = allSystemRoles
                    .Where(r => r.Name == "SalesRepresentative" || r.Name == "Client")
                    .ToList();

                var userExistingRoleNames = await _userManager.GetRolesAsync(user);
                var userExistingRolesTheManagerCannotTypicallyAssign = allSystemRoles
                    .Where(r => userExistingRoleNames.Contains(r.Name) && !(r.Name == "SalesRepresentative" || r.Name == "Client"))
                    .ToList();

                assignableRoles = assignableRoles.Union(userExistingRolesTheManagerCannotTypicallyAssign).Distinct().ToList();
            }
            else
            {
                assignableRoles = new List<Role>(); // Should not happen
            }

            var userRoleIds = new List<int>();
            var userRoles = await _userManager.GetRolesAsync(user); // These are the roles the user currently has
            foreach(var roleName in userRoles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if(role != null) userRoleIds.Add(role.Id);
            }
            model.SelectedRoleIds = userRoleIds; // Pre-select current roles
            // AllRoles should contain all roles that *could* be assigned by this editor OR that the user already has.
            model.AllRoles = new MultiSelectList(assignableRoles.OrderBy(r => r.Name), "Id", "Name", model.SelectedRoleIds);
            model.CurrentRoles = userRoles.ToList(); // Update current roles for display
        }

        // Helper to add Identity errors to ModelState
        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        // GET: Users/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            // Safeguard: Prevent deleting oneself
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id.ToString() == currentUserId)
            {
                ModelState.AddModelError(string.Empty, "Вы не можете удалить свою собственную учетную запись.");
            }

            // Safeguard: Prevent deleting the last admin user
            // This check should ideally be more robust, perhaps checking the count *before* attempting deletion
            // or ensuring there's a mechanism to always have at least one admin.
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count == 1)
                {
                    ModelState.AddModelError(string.Empty, "Нельзя удалить единственного администратора в системе.");
                }
            }

            var userViewModel = new UserViewModel // Using UserViewModel for display consistency
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles // Already fetched
            };

            // If ModelState has errors from safeguards, they will be picked up by ValidationSummary in the View.
            return View(userViewModel);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                // TempData["ErrorMessage"] = "Пользователь не найден.";
                return RedirectToAction(nameof(Index)); // Or return NotFound() if preferred
            }

            // Re-apply safeguards before actual deletion
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id.ToString() == currentUserId)
            {
                // TempData["ErrorMessage"] = "Вы не можете удалить свою собственную учетную запись.";
                // Log this attempt, inform user by redirecting with a message or showing an error view.
                // For now, redirecting to Index. A UserViewModel could be repopulated with an error for the Delete view.
                TempData["ErrorMessage"] = "Удаление собственной учетной записи запрещено.";
                return RedirectToAction(nameof(Index));
            }

            var userRolesForDeleteCheck = await _userManager.GetRolesAsync(user);
            if (userRolesForDeleteCheck.Contains("Admin"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count == 1 && admins.First().Id == user.Id) // Ensure it's THIS user who is the last admin
                {
                    // TempData["ErrorMessage"] = "Нельзя удалить единственного администратора в системе.";
                    // Log this attempt, inform user
                    TempData["ErrorMessage"] = "Удаление единственного администратора запрещено.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Since controller is [Authorize(Roles="Admin")], the current user is Admin.
            // An Admin can delete any other user (except for the safeguards above).
            // If Managers were allowed, more complex role hierarchy checks would be needed here, e.g.:
            // if (User.IsInRole("Manager")) {
            //    if (userRolesForDeleteCheck.Contains("Admin") || userRolesForDeleteCheck.Contains("Manager")) {
            //         TempData["ErrorMessage"] = "Менеджер не может удалить Администратора или другого Менеджера.";
            //         return RedirectToAction(nameof(Index));
            //    }
            // }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                // TempData["SuccessMessage"] = $"Пользователь {user.UserName} успешно удален.";
                return RedirectToAction(nameof(Index));
            }

            // If deletion failed, collect errors and show them, perhaps by returning to the Delete view.
            AddIdentityErrors(result);
            TempData["ErrorMessage"] = $"Ошибка при удалении пользователя {user.UserName}.";
            foreach(var error in result.Errors) { TempData["ErrorMessage"] += " " + error.Description; }
            // It's better to return to the Delete view with the model and errors.
            // For now, this redirects to Index, and errors might be lost if TempData isn't handled there.
            // To return to Delete view:
            // var userViewModel = new UserViewModel { Id = user.Id, UserName = user.UserName, Email = user.Email, Roles = userRolesForDeleteCheck };
            // return View(userViewModel); // This would show errors on the Delete page itself.
            return RedirectToAction(nameof(Index));
        }
    }
}
