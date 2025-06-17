using EventsService.Data; // For ApplicationDbContext
using EventsService.Models; // For User
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // For UserManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // For ToListAsync, Include, etc.
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // For List

// It's anticipated that a ViewModel will be needed for the calendar event data.
// For now, this controller will just be set up. Data preparation is next.
// namespace EventsService.ViewModels { /* CalendarEventViewModel might go here */ }

namespace EventsService.Controllers
{
    [Authorize] // Require login for all actions in this controller
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public StatisticsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Statistics or Statistics/Index
        // Authorization for this Index action will be refined later to include Client and SalesRep
        [Authorize(Roles = "Admin,Manager,SalesRepresentative,Client")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            IQueryable<Order> ordersQuery = _context.Orders
                .Where(o => o.ScheduledServiceDateTime != null) // Only orders with a schedule date
                .Include(o => o.Client)
                .Include(o => o.SalesRepresentative);

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

            // Map to a structure suitable for FullCalendar or other calendar libraries
            // This typically involves properties like id, title, start, end (optional), url.
            var calendarEvents = scheduledOrders.Select(o => new {
                id = o.Id.ToString(), // Calendar event ID (can be OrderId)
                title = $"Заявка #{o.Id} - {(o.Client?.Name ?? "Без клиента")}", // Event title
                start = o.ScheduledServiceDateTime.Value.ToString("o"), // ISO 8601 format for start time
                // end = o.ScheduledServiceDateTime.Value.AddHours(1).ToString("o"), // Optional: if events have a duration, otherwise they are point-in-time
                url = Url.Action("Details", "Orders", new { id = o.Id }) // URL to navigate to when event is clicked
                // You can add more custom properties here if your calendar needs them (e.g., color, description)
            }).ToList();

            // Pass the JSON serialized events to the View.
            // The View will then use JavaScript to initialize the calendar with this data.
            ViewBag.CalendarEventsJson = System.Text.Json.JsonSerializer.Serialize(calendarEvents);

            return View();
        }
    }
}
