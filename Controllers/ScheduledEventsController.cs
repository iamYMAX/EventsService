using EventsService.Data;
using EventsService.Models;
using EventsService.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventsService.Controllers
{
    [Authorize]
    public class ScheduledEventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ScheduledEventsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ScheduledEvents
        [Authorize(Roles = "Admin,Manager,SalesRepresentative,Client")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            IQueryable<ScheduledEvent> eventsQuery = _context.ScheduledEvents
                .Include(se => se.User)
                .Include(se => se.Client)
                .Include(se => se.Order)
                .OrderByDescending(se => se.StartTime);

            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                // No additional filtering needed
            }
            else if (User.IsInRole("SalesRepresentative"))
            {
                eventsQuery = eventsQuery.Where(se => se.UserId == currentUser.Id);
            }
            else if (User.IsInRole("Client"))
            {
                var clientProfile = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == currentUser.Id);
                if (clientProfile != null)
                {
                    eventsQuery = eventsQuery.Where(se => se.ClientId == clientProfile.Id);
                }
                else
                {
                    eventsQuery = Enumerable.Empty<ScheduledEvent>().AsQueryable();
                }
            }
            else
            {
                eventsQuery = Enumerable.Empty<ScheduledEvent>().AsQueryable(); // Should not happen due to [Authorize]
            }

            var scheduledEvents = await eventsQuery.ToListAsync();

            var viewModels = scheduledEvents.Select(se => new ScheduledEventViewModel
            {
                Id = se.Id,
                Title = se.Title,
                Description = se.Description,
                StartTime = se.StartTime,
                EndTime = se.EndTime,
                UserName = se.User != null ? (string.IsNullOrWhiteSpace(se.User.FirstName) && string.IsNullOrWhiteSpace(se.User.LastName) ? se.User.UserName : (se.User.FirstName + " " + se.User.LastName).Trim()) : "N/A",
                ClientName = se.Client?.Name ?? "N/A",
                OrderId = se.OrderId,
                Location = se.Location
            }).ToList();

            return View(viewModels);
        }

        // GET: ScheduledEvents/Details/5
        [Authorize(Roles = "Admin,Manager,SalesRepresentative,Client")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var scheduledEvent = await _context.ScheduledEvents
                .Include(se => se.User)
                .Include(se => se.Client)
                .Include(se => se.Order)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (scheduledEvent == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            bool canView = false;
            if (User.IsInRole("Admin") || User.IsInRole("Manager")) canView = true;
            else if (User.IsInRole("SalesRepresentative") && scheduledEvent.UserId == currentUser.Id) canView = true;
            else if (User.IsInRole("Client"))
            {
                var clientProfile = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == currentUser.Id);
                if (clientProfile != null && scheduledEvent.ClientId == clientProfile.Id) canView = true;
            }

            if (!canView) return Forbid();

            var viewModel = new ScheduledEventViewModel
            {
                Id = scheduledEvent.Id,
                Title = scheduledEvent.Title,
                Description = scheduledEvent.Description,
                StartTime = scheduledEvent.StartTime,
                EndTime = scheduledEvent.EndTime,
                UserName = scheduledEvent.User != null ? (string.IsNullOrWhiteSpace(scheduledEvent.User.FirstName) && string.IsNullOrWhiteSpace(scheduledEvent.User.LastName) ? scheduledEvent.User.UserName : (scheduledEvent.User.FirstName + " " + scheduledEvent.User.LastName).Trim()) : "N/A",
                ClientName = scheduledEvent.Client?.Name ?? "N/A",
                OrderId = scheduledEvent.OrderId,
                Location = scheduledEvent.Location
            };
            return View(viewModel);
        }

        // GET: ScheduledEvents/Create
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")]
        public async Task<IActionResult> Create()
        {
            var viewModel = new ScheduledEventFormViewModel();
            await PopulateSelectListsAsync(viewModel);
            return View(viewModel);
        }

        // POST: ScheduledEvents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")]
        public async Task<IActionResult> Create(ScheduledEventFormViewModel viewModel)
        {
            if (viewModel.EndTime <= viewModel.StartTime) // Ensure custom validation is checked
            {
                ModelState.AddModelError("EndTime", "Время окончания должно быть позже времени начала.");
            }

            if (ModelState.IsValid)
            {
                var scheduledEvent = new ScheduledEvent
                {
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    StartTime = viewModel.StartTime,
                    EndTime = viewModel.EndTime,
                    Location = viewModel.Location,
                    ClientId = viewModel.ClientId,
                    OrderId = viewModel.OrderId,
                    UserId = viewModel.UserId
                };

                var currentUser = await _userManager.GetUserAsync(User);
                if (User.IsInRole("SalesRepresentative") && !User.IsInRole("Admin") && !User.IsInRole("Manager"))
                {
                    if (currentUser != null) // Check if currentUser is not null
                    {
                        scheduledEvent.UserId = currentUser.Id; // Force SalesRep to assign to themselves
                    }
                    else
                    {
                        // Handle the case where currentUser is null, perhaps log or add model error
                        ModelState.AddModelError("", "Не удалось определить текущего пользователя.");
                        await PopulateSelectListsAsync(viewModel, viewModel.UserId, viewModel.ClientId, viewModel.OrderId);
                        return View(viewModel);
                    }
                }

                _context.Add(scheduledEvent);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PopulateSelectListsAsync(viewModel, viewModel.UserId, viewModel.ClientId, viewModel.OrderId);
            return View(viewModel);
        }

        // GET: ScheduledEvents/Edit/5
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var scheduledEvent = await _context.ScheduledEvents.FindAsync(id);
            if (scheduledEvent == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge(); // Added null check for currentUser

            bool canEdit = User.IsInRole("Admin") || User.IsInRole("Manager") || (User.IsInRole("SalesRepresentative") && scheduledEvent.UserId == currentUser.Id);
            if (!canEdit) return Forbid();

            var viewModel = new ScheduledEventFormViewModel
            {
                Id = scheduledEvent.Id,
                Title = scheduledEvent.Title,
                Description = scheduledEvent.Description,
                StartTime = scheduledEvent.StartTime,
                EndTime = scheduledEvent.EndTime,
                Location = scheduledEvent.Location,
                UserId = scheduledEvent.UserId,
                ClientId = scheduledEvent.ClientId,
                OrderId = scheduledEvent.OrderId
            };
            await PopulateSelectListsAsync(viewModel, scheduledEvent.UserId, scheduledEvent.ClientId, scheduledEvent.OrderId);
            return View(viewModel);
        }

        // POST: ScheduledEvents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")]
        public async Task<IActionResult> Edit(int id, ScheduledEventFormViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (viewModel.EndTime <= viewModel.StartTime)
            {
                ModelState.AddModelError("EndTime", "Время окончания должно быть позже времени начала.");
            }

            var scheduledEventToUpdate = await _context.ScheduledEvents.FindAsync(id);
            if (scheduledEventToUpdate == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge(); // Added null check

            bool canEditFully = User.IsInRole("Admin") || User.IsInRole("Manager");
            bool isOwnerSalesRep = User.IsInRole("SalesRepresentative") && scheduledEventToUpdate.UserId == currentUser.Id;

            if (!canEditFully && !isOwnerSalesRep) return Forbid();

            if (ModelState.IsValid)
            {
                scheduledEventToUpdate.Title = viewModel.Title;
                scheduledEventToUpdate.Description = viewModel.Description;
                scheduledEventToUpdate.StartTime = viewModel.StartTime;
                scheduledEventToUpdate.EndTime = viewModel.EndTime;
                scheduledEventToUpdate.Location = viewModel.Location;
                scheduledEventToUpdate.ClientId = viewModel.ClientId; // Nullable
                scheduledEventToUpdate.OrderId = viewModel.OrderId;   // Nullable

                if (canEditFully) // Admin or Manager can change UserId
                {
                    scheduledEventToUpdate.UserId = viewModel.UserId; // Nullable
                }
                // SalesRep cannot change UserId once set (or if they are not Admin/Manager)
                // If it was their event, UserId remains their Id. This logic assumes SalesRep cannot re-assign.

                try
                {
                    _context.Update(scheduledEventToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ScheduledEvents.Any(e => e.Id == scheduledEventToUpdate.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Details), new { id = scheduledEventToUpdate.Id });
            }
            await PopulateSelectListsAsync(viewModel, viewModel.UserId, viewModel.ClientId, viewModel.OrderId);
            return View(viewModel);
        }

        // GET: ScheduledEvents/Delete/5
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var scheduledEvent = await _context.ScheduledEvents
                .Include(se => se.User)
                .Include(se => se.Client)
                .Include(se => se.Order)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (scheduledEvent == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge(); // Added null check

            bool canDelete = User.IsInRole("Admin") || User.IsInRole("Manager") || (User.IsInRole("SalesRepresentative") && scheduledEvent.UserId == currentUser.Id);
            if (!canDelete) return Forbid();

            var viewModel = new ScheduledEventViewModel
            {
                 Id = scheduledEvent.Id,
                Title = scheduledEvent.Title,
                Description = scheduledEvent.Description,
                StartTime = scheduledEvent.StartTime,
                EndTime = scheduledEvent.EndTime,
                UserName = scheduledEvent.User != null ? (string.IsNullOrWhiteSpace(scheduledEvent.User.FirstName) && string.IsNullOrWhiteSpace(scheduledEvent.User.LastName) ? scheduledEvent.User.UserName : (scheduledEvent.User.FirstName + " " + scheduledEvent.User.LastName).Trim()) : "N/A",
                ClientName = scheduledEvent.Client?.Name ?? "N/A",
                OrderId = scheduledEvent.OrderId,
                Location = scheduledEvent.Location
            };
            return View(viewModel);
        }

        // POST: ScheduledEvents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var scheduledEvent = await _context.ScheduledEvents.FindAsync(id);
            if (scheduledEvent == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge(); // Added null check

            bool canDelete = User.IsInRole("Admin") || User.IsInRole("Manager") || (User.IsInRole("SalesRepresentative") && scheduledEvent.UserId == currentUser.Id);

            if (!canDelete) return Forbid();

            _context.ScheduledEvents.Remove(scheduledEvent);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSelectListsAsync(ScheduledEventFormViewModel viewModel, int? selectedUserId = null, int? selectedClientId = null, int? selectedOrderId = null)
        {
            // Users (Sales Reps, potentially Managers)
            var assignableUsers = new List<User>();
            var salesRepUsers = await _userManager.GetUsersInRoleAsync("SalesRepresentative");
            assignableUsers.AddRange(salesRepUsers);
            var managerUsers = await _userManager.GetUsersInRoleAsync("Manager");
            assignableUsers.AddRange(managerUsers);
            assignableUsers = assignableUsers.DistinctBy(u => u.Id).OrderBy(u => u.UserName).ToList();

            viewModel.Users = new SelectList(assignableUsers.Select(u => new {
                u.Id,
                DisplayText = $"{(string.IsNullOrWhiteSpace(u.FirstName) && string.IsNullOrWhiteSpace(u.LastName) ? u.UserName : (u.FirstName + " " + u.LastName).Trim())} ({(_userManager.GetRolesAsync(u).Result.FirstOrDefault() ?? "User")})"
            }), "Id", "DisplayText", selectedUserId);

            // Clients
            viewModel.Clients = new SelectList(await _context.Clients.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", selectedClientId);

            // Orders (simplified: shows ID. Could be more descriptive)
            viewModel.Orders = new SelectList(await _context.Orders.OrderByDescending(o => o.OrderDate).Select(o => new {
                o.Id,
                OrderDisplay = $"Заявка #{o.Id} от {o.OrderDate:dd.MM.yyyy}"
            }).ToListAsync(), "Id", "OrderDisplay", selectedOrderId);
        }
    }
}
