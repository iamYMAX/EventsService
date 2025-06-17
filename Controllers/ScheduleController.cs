using EventsService.Data;
using EventsService.Models; // For User, Client, Order
using EventsService.ViewModels; // Added using statement
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // For List

// Placeholder for OrderScheduleViewModel - will be formally created in next step
// For now, the controller will project to an anonymous type or a simple local class.
namespace EventsService.Controllers
{
    [Authorize]
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ScheduleController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Schedule or Schedule/Index
        [Authorize(Roles = "Admin,Manager,SalesRepresentative,Client")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            IQueryable<Order> ordersQuery = _context.Orders
                .Where(o => o.ScheduledServiceDateTime != null) // Only orders with a schedule date
                .Include(o => o.Client)
                .Include(o => o.SalesRepresentative)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product); // For OrderSummary

            // Apply role-based filtering
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                // No additional filtering
            }
            else if (User.IsInRole("SalesRepresentative"))
            {
                ordersQuery = ordersQuery.Where(o => o.SalesRepresentativeId == currentUser.Id);
            }
            else if (User.IsInRole("Client"))
            {
                var clientProfile = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == currentUser.Id);
                if (clientProfile != null)
                {
                    ordersQuery = ordersQuery.Where(o => o.ClientId == clientProfile.Id);
                }
                else
                {
                    ordersQuery = Enumerable.Empty<Order>().AsQueryable();
                }
            }
            else
            {
                // Should not be reached if [Authorize] is effective with specified roles
                ordersQuery = Enumerable.Empty<Order>().AsQueryable();
            }

            var scheduledOrders = await ordersQuery
                .OrderBy(o => o.ScheduledServiceDateTime) // Order by the schedule date
                .ToListAsync();

            // Map to ViewModel (using the local placeholder class for now)
            var viewModels = scheduledOrders.Select(o => new OrderScheduleViewModel // Changed to OrderScheduleViewModel
            {
                OrderId = o.Id,
                ScheduledServiceDateTime = o.ScheduledServiceDateTime.Value, // .Value because we filtered for non-null
                ClientName = o.Client?.Name ?? "N/A",
                SalesRepresentativeName = o.SalesRepresentative != null ?
                    (string.IsNullOrWhiteSpace(o.SalesRepresentative.FirstName) && string.IsNullOrWhiteSpace(o.SalesRepresentative.LastName) ?
                        o.SalesRepresentative.UserName :
                        (o.SalesRepresentative.FirstName + " " + o.SalesRepresentative.LastName).Trim())
                    : "N/A",
                OrderStatus = o.Status.ToString(),
                OrderSummary = o.OrderItems != null && o.OrderItems.Any() ?
                    string.Join(", ", o.OrderItems.Select(oi => oi.Product?.Name ?? "Товар без имени").Take(2)) + (o.OrderItems.Count > 2 ? "..." : "") :
                    "Нет позиций"
            }).ToList();

            return View(viewModels);
        }
    }
}
