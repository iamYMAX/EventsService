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
            IQueryable<Order> orders = _context.Orders
                                            .Include(o => o.Client)
                                            .Include(o => o.SalesRepresentative)
                                            .OrderByDescending(o => o.OrderDate);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge(); // Should not happen if [Authorize] is effective
            }

            if (User.IsInRole("Client"))
            {
                // Ensure Client navigation property is loaded for the current user
                var userWithClient = await _context.Users.Include(u => u.Client)
                                                 .FirstOrDefaultAsync(u => u.Id == currentUser.Id);

                if (userWithClient?.Client?.Id != null)
                {
                    orders = orders.Where(o => o.ClientId == userWithClient.Client.Id);
                }
                else
                {
                    // Client user not linked to a Client record, or Client entity is null.
                    orders = Enumerable.Empty<Order>().AsQueryable();
                }
            }
            else if (User.IsInRole("SalesRepresentative"))
            {
                orders = orders.Where(o => o.SalesRepresentativeId == currentUser.Id);
            }
            // Admins and Managers see all orders by default due to broader role access

            return View(await orders.ToListAsync());
        }


        // GET: Orders/Create
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateOrderViewModel
            {
                Clients = new SelectList(await _context.Clients.OrderBy(c => c.Name).ToListAsync(), "Id", "Name"),
                // SalesRepresentatives will be populated below
                Products = new MultiSelectList(await _context.Products.OrderBy(p => p.Name).ToListAsync(), "Id", "Name"),
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.New
            };

            var salesRepUsers = await _userManager.GetUsersInRoleAsync("SalesRepresentative");
            var salesRepListItems = salesRepUsers.Select(user => new SelectListItem
            {
                Value = user.Id.ToString(),
                Text = string.IsNullOrWhiteSpace(user.LastName) && string.IsNullOrWhiteSpace(user.FirstName)
                       ? user.UserName
                       : $"{user.LastName} {user.FirstName} ({user.Position})"
            }).OrderBy(item => item.Text).ToList();

            viewModel.SalesRepresentatives = new SelectList(salesRepListItems, "Value", "Text");

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
            // viewModel.SalesRepresentatives will be populated below
            viewModel.Products = new MultiSelectList(await _context.Products.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", viewModel.SelectedProductIds);

            var salesRepUsersForPost = await _userManager.GetUsersInRoleAsync("SalesRepresentative");
            var salesRepListItemsForPost = salesRepUsersForPost.Select(user => new SelectListItem
            {
                Value = user.Id.ToString(),
                Text = string.IsNullOrWhiteSpace(user.LastName) && string.IsNullOrWhiteSpace(user.FirstName)
                       ? user.UserName
                       : $"{user.LastName} {user.FirstName} ({user.Position})"
            }).OrderBy(item => item.Text).ToList();
            viewModel.SalesRepresentatives = new SelectList(salesRepListItemsForPost, "Value", "Text", viewModel.SalesRepresentativeId);

            return View(viewModel);
        }
    }
}
