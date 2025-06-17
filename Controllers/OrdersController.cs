using EventsService.Data;
using EventsService.Models;
using EventsService.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Required for List
using System; // Required for DateTime

namespace EventsService.Controllers
{
    [Authorize] // General authorization, can be refined per action
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public OrdersController(ApplicationDbContext context, UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Orders/Index
        [Authorize(Roles = "Admin,Manager,SalesRepresentative,Client")]
        public async Task<IActionResult> Index()
        {
            IQueryable<Order> ordersQuery = _context.Orders
                                                .Include(o => o.Client)
                                                .Include(o => o.SalesRepresentative) // User object for SalesRep
                                                .OrderByDescending(o => o.OrderDate);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) // Should not happen if [Authorize] is effective
            {
                return Challenge();
            }

            if (User.IsInRole("Client"))
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
            else if (User.IsInRole("SalesRepresentative"))
            {
                ordersQuery = ordersQuery.Where(o => o.SalesRepresentativeId == currentUser.Id);
            }

            var orders = await ordersQuery.ToListAsync();
            var orderViewModels = new List<OrderViewModel>();

            foreach (var order in orders)
            {
                var vm = new OrderViewModel
                {
                    Id = order.Id,
                    ClientName = order.Client?.Name ?? "N/A", // Handle null client if possible
                    OrderDate = order.OrderDate,
                    Status = order.Status
                };

                if (order.SalesRepresentative != null)
                {
                    var roles = await _userManager.GetRolesAsync(order.SalesRepresentative);
                    var primaryRole = roles.FirstOrDefault() ?? "Сотрудник";
                    vm.SalesRepresentativeDisplay = $"{order.SalesRepresentative.UserName} ({primaryRole})";
                }
                else
                {
                    vm.SalesRepresentativeDisplay = "Не назначен";
                }
                orderViewModels.Add(vm);
            }
            return View(orderViewModels);
        }


        // GET: Orders/Create
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create()
        {
            var salesRepUsers = await _userManager.GetUsersInRoleAsync("SalesRepresentative");
            // Consider including other roles like "Manager" if they can also be sales reps
            // var managerUsers = await _userManager.GetUsersInRoleAsync("Manager");
            // var allPotentialReps = salesRepUsers.Concat(managerUsers).DistinctBy(u => u.Id);

            var salesRepSelectListItems = new List<SelectListItem>();
            foreach (var user in salesRepUsers.OrderBy(u => u.UserName)) // Using salesRepUsers, adjust if allPotentialReps is used
            {
                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? "Сотрудник"; // Default if no role
                salesRepSelectListItems.Add(new SelectListItem
                {
                    Value = user.Id.ToString(),
                    Text = $"{user.UserName} ({primaryRole})"
                });
            }

            var viewModel = new CreateOrderViewModel
            {
                Clients = new SelectList(await _context.Clients.OrderBy(c => c.Name).ToListAsync(), "Id", "Name"),
                SalesRepresentatives = new SelectList(salesRepSelectListItems, "Value", "Text"),
                Products = new MultiSelectList(await _context.Products.OrderBy(p => p.Name).ToListAsync(), "Id", "Name"),
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.New
            };
            return View(viewModel);
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(CreateOrderViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var order = new Order
                {
                    ClientId = viewModel.ClientId,
                    SalesRepresentativeId = viewModel.SalesRepresentativeId,
                    OrderDate = viewModel.OrderDate,
                    Status = viewModel.Status,
                    OrderItems = new List<OrderItem>()
                };

                if (viewModel.SelectedProductIds != null && viewModel.SelectedProductIds.Any())
                {
                    foreach (var productId in viewModel.SelectedProductIds)
                    {
                        var product = await _context.Products.FindAsync(productId);
                        if (product != null)
                        {
                            order.OrderItems.Add(new OrderItem
                            {
                                ProductId = product.Id,
                                Quantity = 1, // Default quantity to 1 for now
                                PriceAtTimeOfOrder = product.Price
                            });
                        }
                    }
                }
                // else: Handle case where no products are selected if it's possible despite [Required]
                // The [Required] on SelectedProductIds should prevent this if client-side validation works.
                // If it can be empty, then the OrderItems list will be empty.

                _context.Add(order);
                await _context.SaveChangesAsync();
                // TempData["SuccessMessage"] = "Заявка успешно создана!";
                return RedirectToAction(nameof(Index));
            }

            // If model state is invalid, re-populate dropdowns
            viewModel.Clients = new SelectList(await _context.Clients.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", viewModel.ClientId);
            // Re-populate SalesRepresentatives with formatted text
            var salesRepUsersForRepopulate = await _userManager.GetUsersInRoleAsync("SalesRepresentative");
            var salesRepSelectListItemsForRepopulate = new List<SelectListItem>();
            foreach (var user in salesRepUsersForRepopulate.OrderBy(u => u.UserName))
            {
                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? "Сотрудник";
                salesRepSelectListItemsForRepopulate.Add(new SelectListItem
                {
                    Value = user.Id.ToString(),
                    Text = $"{user.UserName} ({primaryRole})"
                });
            }
            viewModel.SalesRepresentatives = new SelectList(salesRepSelectListItemsForRepopulate, "Value", "Text", viewModel.SalesRepresentativeId);
            viewModel.Products = new MultiSelectList(await _context.Products.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", viewModel.SelectedProductIds);
            return View(viewModel);
        }

        // GET: Orders/Details/5
        [Authorize(Roles = "Admin,Manager,SalesRepresentative,Client")] // Adjust roles as needed
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.SalesRepresentative)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product) // Example: include order items and products
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge(); // Should not happen

            bool canView = false;
            if (User.IsInRole("Admin") || User.IsInRole("Manager")) canView = true;
            else if (User.IsInRole("SalesRepresentative") && order.SalesRepresentativeId == currentUser.Id) canView = true;
            else if (User.IsInRole("Client"))
            {
                 var clientProfile = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == currentUser.Id);
                 if (clientProfile != null && order.ClientId == clientProfile.Id) canView = true;
            }
            if (!canView) return Forbid();


            var orderViewModel = new OrderViewModel
            {
                Id = order.Id,
                ClientName = order.Client?.Name ?? "N/A",
                OrderDate = order.OrderDate,
                Status = order.Status
                // You would also map OrderItems to a ViewModel property if showing them here
            };

            if (order.SalesRepresentative != null)
            {
                var roles = await _userManager.GetRolesAsync(order.SalesRepresentative);
                var primaryRole = roles.FirstOrDefault() ?? "Сотрудник";
                orderViewModel.SalesRepresentativeDisplay = $"{order.SalesRepresentative.UserName} ({primaryRole})";
            }
            else
            {
                orderViewModel.SalesRepresentativeDisplay = "Не назначен";
            }

            // Pass other details like OrderItems through ViewBag or extend OrderViewModel
            ViewBag.OrderItems = order.OrderItems;

            return View(orderViewModel);
        }
    }
}
