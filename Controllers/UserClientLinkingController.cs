using EventsService.Data;
using EventsService.Models;
using EventsService.ViewModels; // Now needed for UserClientLinkingIndexViewModel etc.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EventsService.Controllers
{
    [Authorize(Roles = "Admin")] // Only Admins can access this controller
    public class UserClientLinkingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public UserClientLinkingController(ApplicationDbContext context,
                                           UserManager<User> userManager,
                                           RoleManager<Role> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: UserClientLinking
        public async Task<IActionResult> Index()
        {
            var viewModel = new UserClientLinkingIndexViewModel();

            var allUsers = await _userManager.Users
                                    .OrderBy(u => u.UserName)
                                    .ToListAsync();
            var allClientProfiles = await _context.Clients
                                    .Include(c => c.User) // Include User to get their name if linked
                                    .OrderBy(c => c.Name)
                                    .ToListAsync();

            var clientRole = await _roleManager.FindByNameAsync("Client");
            if (clientRole == null) {
                // Handle role not found - critical error, perhaps add a model error or log
                ModelState.AddModelError(string.Empty, "Критическая ошибка: Роль 'Клиент' не найдена в системе.");
                return View(viewModel); // Return with error
            }

            // Using HashSet for efficient lookup of client IDs that are already linked to a user in the "Client" role
            var linkedClientProfileIds = new HashSet<int>();

            foreach (var user in allUsers)
            {
                bool isInClientRole = await _userManager.IsInRoleAsync(user, "Client");
                // Find if this user is linked to any client profile via Client.UserId
                var linkedClientProfileForThisUser = allClientProfiles.FirstOrDefault(c => c.UserId == user.Id);

                string userDisplayName = (user.FirstName + " " + user.LastName).Trim();
                if (string.IsNullOrWhiteSpace(userDisplayName))
                {
                    userDisplayName = user.UserName ?? "N/A";
                }

                if (linkedClientProfileForThisUser != null)
                {
                    // This user is linked to a client profile.
                    if (isInClientRole) // If the user is also in "Client" role, it's a correctly linked pair.
                    {
                        viewModel.LinkedUserClientPairs.Add(new UserToLinkViewModel
                        {
                            UserId = user.Id,
                            UserName = userDisplayName, // Display name for list
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            IsAlreadyLinkedToClient = true,
                            LinkedClientId = linkedClientProfileForThisUser.Id,
                            LinkedClientName = linkedClientProfileForThisUser.Name
                        });
                        linkedClientProfileIds.Add(linkedClientProfileForThisUser.Id);
                    }
                    // If user is linked but NOT in "Client" role, they are not added to any list on this page for now.
                    // This page focuses on users who *should* be clients or are already clients.
                }
                else // This user is NOT linked to any client profile (Client.UserId does not point to them).
                {
                    if (isInClientRole) // If they are in "Client" role, they are an unlinked user needing a profile.
                    {
                        viewModel.UnlinkedClientRoleUsers.Add(new UserToLinkViewModel
                        {
                            UserId = user.Id,
                            UserName = userDisplayName,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            IsAlreadyLinkedToClient = false
                        });
                    }
                }
            }

            // Unlinked client profiles are those that do not have a UserId set at all.
            viewModel.UnlinkedClientProfiles = allClientProfiles
                .Where(c => c.UserId == null)
                .Select(c => new ClientToLinkViewModel
                {
                    ClientId = c.Id,
                    ClientName = c.Name,
                    IsAlreadyLinkedToUser = false // Because UserId is null
                }).ToList();

            // Populate SelectLists for forms using the categorized lists
            viewModel.AvailableUnlinkedUsers = new SelectList(
                viewModel.UnlinkedClientRoleUsers.Select(u => new {
                    u.UserId,
                    // Use the already constructed display name for consistency
                    DisplayText = $"{u.UserName}"
                }).OrderBy(u => u.DisplayText),
                "UserId",
                "DisplayText"
            );

            viewModel.AvailableUnlinkedClients = new SelectList(
                viewModel.UnlinkedClientProfiles.OrderBy(c => c.ClientName),
                nameof(ClientToLinkViewModel.ClientId),
                nameof(ClientToLinkViewModel.ClientName)
            );

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkUserToClient(UserClientLinkingIndexViewModel model)
        {
            if (model.SelectedUserIdToLink == null || model.SelectedClientIdToLink == null)
            {
                ModelState.AddModelError("", "Необходимо выбрать пользователя и профиль клиента для связывания.");
                // Repopulate necessary data for the Index view if returning directly to it with errors
                // This might involve calling a method similar to the Index GET action's data population.
                // For simplicity now, we'll just redirect, but ideally, repopulate and return View("Index", model).
                TempData["ErrorMessage"] = "Ошибка: Пользователь или клиент не выбран.";
                return RedirectToAction(nameof(Index));
            }

            var userToLink = await _userManager.FindByIdAsync(model.SelectedUserIdToLink.Value.ToString());
            var clientToLink = await _context.Clients.FindAsync(model.SelectedClientIdToLink.Value);

            if (userToLink == null || clientToLink == null)
            {
                TempData["ErrorMessage"] = "Ошибка: Пользователь или клиент не найден.";
                return RedirectToAction(nameof(Index));
            }

            // Check if user is in Client role (optional, but good practice)
            if (!await _userManager.IsInRoleAsync(userToLink, "Client"))
            {
                 TempData["ErrorMessage"] = $"Ошибка: Пользователь {userToLink.UserName} не состоит в роли 'Клиент'.";
                 return RedirectToAction(nameof(Index));
            }

            if (clientToLink.UserId != null)
            {
                TempData["ErrorMessage"] = $"Ошибка: Профиль клиента '{clientToLink.Name}' уже связан с другим пользователем.";
                return RedirectToAction(nameof(Index));
            }

            // Check if this user is already linked to another client
            var existingLinkForUser = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userToLink.Id);
            if (existingLinkForUser != null)
            {
                 TempData["ErrorMessage"] = $"Ошибка: Пользователь {userToLink.UserName} уже связан с профилем клиента '{existingLinkForUser.Name}'.";
                 return RedirectToAction(nameof(Index));
            }


            clientToLink.UserId = userToLink.Id;
            _context.Update(clientToLink);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Пользователь {userToLink.UserName} успешно связан с клиентом {clientToLink.Name}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClientForUser(UserClientLinkingIndexViewModel model)
        {
            if (model.SelectedUserIdToLink == null)
            {
                ModelState.AddModelError("SelectedUserIdToLink", "Необходимо выбрать пользователя.");
            }
            if (string.IsNullOrWhiteSpace(model.NewClientName))
            {
                ModelState.AddModelError("NewClientName", "Имя нового клиента обязательно для заполнения.");
            }

            if (!ModelState.IsValid) // Checks both explicit errors and data annotation errors on NewClient... fields
            {
                TempData["ErrorMessage"] = "Ошибка валидации при создании нового профиля клиента.";
                // Ideally, repopulate and return View("Index", model) here.
                // For now, redirecting, but this means form values and specific errors are lost.
                // To properly return to view, the Index action's full data loading logic would be needed here or in a helper.
                return RedirectToAction(nameof(Index)); // Simplified for now
            }

            var userToLink = await _userManager.FindByIdAsync(model.SelectedUserIdToLink.Value.ToString());
            if (userToLink == null)
            {
                TempData["ErrorMessage"] = "Ошибка: Пользователь для привязки не найден.";
                return RedirectToAction(nameof(Index));
            }

            // Check if user is in Client role
            if (!await _userManager.IsInRoleAsync(userToLink, "Client"))
            {
                 TempData["ErrorMessage"] = $"Ошибка: Пользователь {userToLink.UserName} не состоит в роли 'Клиент'.";
                 return RedirectToAction(nameof(Index));
            }

            // Check if this user is already linked to another client
            var existingLinkForUser = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userToLink.Id);
            if (existingLinkForUser != null)
            {
                 TempData["ErrorMessage"] = $"Ошибка: Пользователь {userToLink.UserName} уже связан с профилем клиента '{existingLinkForUser.Name}'. Нельзя создать еще один.";
                 return RedirectToAction(nameof(Index));
            }

            var newClient = new Client
            {
                Name = model.NewClientName!, // Null check done by ModelState.IsValid for Required
                PhoneNumber = model.NewClientPhoneNumber,
                Organization = model.NewClientOrganization,
                Email = model.NewClientEmail,
                UserId = userToLink.Id // Link to the selected user
            };

            _context.Clients.Add(newClient);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Новый профиль клиента '{newClient.Name}' успешно создан и связан с пользователем {userToLink.UserName}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlinkClientFromUser(int clientId) // Changed from model to direct clientId
        {
            if (clientId <= 0)
            {
                TempData["ErrorMessage"] = "Ошибка: Некорректный ID клиента.";
                return RedirectToAction(nameof(Index));
            }

            var clientToUnlink = await _context.Clients.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == clientId);

            if (clientToUnlink == null)
            {
                TempData["ErrorMessage"] = "Ошибка: Профиль клиента не найден.";
                return RedirectToAction(nameof(Index));
            }

            if (clientToUnlink.UserId == null)
            {
                TempData["WarningMessage"] = $"Профиль клиента '{clientToUnlink.Name}' уже не связан с пользователем.";
                return RedirectToAction(nameof(Index));
            }

            var linkedUserName = clientToUnlink.User?.UserName ?? "неизвестному пользователю";

            clientToUnlink.UserId = null;
            // clientToUnlink.User = null; // EF Core should handle this if UserId is set to null
            _context.Update(clientToUnlink);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Профиль клиента '{clientToUnlink.Name}' успешно отвязан от пользователя ({linkedUserName}).";
            return RedirectToAction(nameof(Index));
        }
    }
}
