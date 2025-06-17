// Path: Controllers/OrdersController.cs
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
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        // private readonly SignInManager<User> _signInManager; // Not used in provided snippets

        public OrdersController(ApplicationDbContext context, UserManager<User> userManager/*, SignInManager<User> signInManager*/)
        {
            _context = context;
            _userManager = userManager;
            // _signInManager = signInManager;
        }

        // GET: Orders/Index
        [Authorize(Roles = "Admin,Manager,SalesRepresentative,Client")]
        public async Task<IActionResult> Index()
        {
            IQueryable<Order> ordersQuery = _context.Orders
                                                .Include(o => o.Client)
                                                .Include(o => o.SalesRepresentative)
                                                .OrderByDescending(o => o.OrderDate);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (User.IsInRole("Client"))
            {
                var clientProfile = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == currentUser.Id); // Assuming Client has UserId
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
                // Corrected: Both SalesRepresentativeId and currentUser.Id are strings
                ordersQuery = ordersQuery.Where(o => o.SalesRepresentativeId == currentUser.Id);
            }

            var orders = await ordersQuery.ToListAsync();
            var orderViewModels = new List<OrderViewModel>(); // Assuming OrderViewModel exists and is suitable

            foreach (var order in orders)
            {
                var vm = new OrderViewModel // Map to your OrderViewModel
                {
                    Id = order.Id,
                    ClientName = order.Client?.Name ?? "N/A",
                    OrderDate = order.OrderDate,
                    Status = order.Status
                };
                if (order.SalesRepresentative != null)
                {
                    var roles = await _userManager.GetRolesAsync(order.SalesRepresentative);
                    var primaryRole = roles.FirstOrDefault() ?? "Сотрудник";
                    vm.SalesRepresentativeDisplay = $"{order.SalesRepresentative.UserName} ({primaryRole})";
                }
                else { vm.SalesRepresentativeDisplay = "Не назначен"; }
                orderViewModels.Add(vm);
            }
            return View(orderViewModels);
        }

        // GET: Orders/Create
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")]
        public async Task<IActionResult> Create()
        {
            var salesRepUsers = await _userManager.GetUsersInRoleAsync("SalesRepresentative");
            var salesRepSelectListItems = salesRepUsers.OrderBy(u => u.UserName).Select(user => {
                var roles = _userManager.GetRolesAsync(user).Result; // Be cautious with .Result in async; consider await
                var primaryRole = roles.FirstOrDefault() ?? "Сотрудник";
                return new SelectListItem { Value = user.Id.ToString(), Text = $"{user.UserName} ({primaryRole})" };
            }).ToList();

            var products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            var viewModel = new CreateOrderViewModel
            {
                Clients = new SelectList(await _context.Clients.OrderBy(c => c.Name).ToListAsync(), "Id", "Name"),
                SalesRepresentatives = new SelectList(salesRepSelectListItems, "Value", "Text"),
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.New,
                ProductList = new SelectList(products, "Id", "Name"),
                ProductDetailsForJs = products.Select(p => new ProductInfoForJs(p.Id.ToString(), p.Name, p.Price)).ToList()
            };
            return View(viewModel);
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")]
        public async Task<IActionResult> Create(CreateOrderViewModel viewModel)
        {
            if (viewModel.OrderItems == null || !viewModel.OrderItems.Any())
            {
                ModelState.AddModelError("OrderItems", "Please add at least one product to the order.");
            }

            if (ModelState.IsValid)
            {
                var order = new Order
                {
                    ClientId = viewModel.ClientId,
                    OrderDate = viewModel.OrderDate,
                    Status = viewModel.Status,
                    OrderItems = new List<OrderItem>()
                };

                var currentUser = await _userManager.GetUserAsync(User);
                if (User.IsInRole("SalesRepresentative") && currentUser != null)
                {
                    order.SalesRepresentativeId = currentUser.Id; // String to String
                }
                else
                {
                    order.SalesRepresentativeId = viewModel.SalesRepresentativeId; // String to String
                }

                foreach (var itemVM in viewModel.OrderItems)
                {
                    var product = await _context.Products.FindAsync(itemVM.ProductId);
                    if (product != null)
                    {
                        order.OrderItems.Add(new OrderItem { ProductId = product.Id, Quantity = itemVM.Quantity, PriceAtTimeOfOrder = product.Price });
                    }
                    else { ModelState.AddModelError("", $"Product with ID {itemVM.ProductId} not found."); }
                }

                if (ModelState.IsValid) // Re-check after product validation
                {
                    _context.Add(order);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            await RepopulateViewModelForCreateError(viewModel); // Helper for repopulating SelectLists
            return View(viewModel);
        }

        // GET: Orders/Edit/5
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var order = await _context.Orders.Include(o => o.Client).Include(o => o.SalesRepresentative).Include(o => o.OrderItems).ThenInclude(oi => oi.Product).FirstOrDefaultAsync(m => m.Id == id);
            if (order == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();
            bool canEdit = User.IsInRole("Admin") || User.IsInRole("Manager") || (User.IsInRole("SalesRepresentative") && order.SalesRepresentativeId == currentUser.Id); // String to String
            if (!canEdit) return Forbid();

            var productsForEdit = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            var viewModel = new EditOrderViewModel
            {
                Id = order.Id, ClientId = order.ClientId, SalesRepresentativeId = order.SalesRepresentativeId, // String to String
                OrderDate = order.OrderDate, Status = order.Status,
                OrderItems = order.OrderItems.Select(oi => new OrderItemViewModel { ProductId = oi.ProductId, Quantity = oi.Quantity, ProductName = oi.Product?.Name, ProductPrice = oi.PriceAtTimeOfOrder }).ToList(),
                Clients = new SelectList(await _context.Clients.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", order.ClientId),
                SalesRepresentatives = new SelectList((await _userManager.GetUsersInRoleAsync("SalesRepresentative")).OrderBy(u => u.UserName).Select(u => new SelectListItem { Value = u.Id, Text = $"{u.UserName} ({(_userManager.GetRolesAsync(u).Result.FirstOrDefault() ?? "Сотрудник")})" }), "Value", "Text", order.SalesRepresentativeId),
                Statuses = new SelectList(Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString() }), "Value", "Text", order.Status.ToString()),
                ProductList = new SelectList(productsForEdit, "Id", "Name"),
                ProductDetailsForJs = productsForEdit.Select(p => new ProductInfoForJs(p.Id.ToString(), p.Name, p.Price)).ToList()
            };
            return View(viewModel);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")]
        public async Task<IActionResult> Edit(int id, EditOrderViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();
            if (viewModel.OrderItems == null || !viewModel.OrderItems.Any())
            {
                 ModelState.AddModelError("OrderItems", "Please add at least one product to the order.");
            }

            if (ModelState.IsValid)
            {
                var orderToUpdate = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);
                if (orderToUpdate == null) return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();
                bool canEdit = User.IsInRole("Admin") || User.IsInRole("Manager") || (User.IsInRole("SalesRepresentative") && orderToUpdate.SalesRepresentativeId == currentUser.Id); // String to String
                if (!canEdit) return Forbid();

                orderToUpdate.ClientId = viewModel.ClientId;
                orderToUpdate.OrderDate = viewModel.OrderDate;
                orderToUpdate.Status = viewModel.Status;
                if (User.IsInRole("Admin") || User.IsInRole("Manager")) { orderToUpdate.SalesRepresentativeId = viewModel.SalesRepresentativeId; } // String to String

                _context.OrderItems.RemoveRange(orderToUpdate.OrderItems);
                orderToUpdate.OrderItems = new List<OrderItem>();
                foreach (var itemVM in viewModel.OrderItems)
                {
                    var product = await _context.Products.FindAsync(itemVM.ProductId);
                    if (product != null) { orderToUpdate.OrderItems.Add(new OrderItem { OrderId = orderToUpdate.Id, ProductId = product.Id, Quantity = itemVM.Quantity, PriceAtTimeOfOrder = product.Price }); }
                    else { ModelState.AddModelError("", $"Product with ID {itemVM.ProductId} not found."); }
                }

                if (ModelState.IsValid) // Re-check
                {
                    try { _context.Update(orderToUpdate); await _context.SaveChangesAsync(); }
                    catch (DbUpdateConcurrencyException) { if (!OrderExists(orderToUpdate.Id)) return NotFound(); else throw; }
                    return RedirectToAction(nameof(Details), new { id = orderToUpdate.Id });
                }
            }
            await RepopulateViewModelForEditError(viewModel); // Helper
            return View(viewModel);
        }

        // GET: Orders/Details/5 (Example, ensure SalesRepresentativeId comparison is string to string)
        [Authorize(Roles = "Admin,Manager,SalesRepresentative,Client")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var order = await _context.Orders.Include(o => o.Client).Include(o => o.SalesRepresentative).Include(o => o.OrderItems).ThenInclude(oi => oi.Product).FirstOrDefaultAsync(m => m.Id == id);
            if (order == null) return NotFound();
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();
            bool canView = User.IsInRole("Admin") || User.IsInRole("Manager") || (User.IsInRole("SalesRepresentative") && order.SalesRepresentativeId == currentUser.Id) || (User.IsInRole("Client") && (await _context.Clients.FirstOrDefaultAsync(c => c.UserId == currentUser.Id))?.Id == order.ClientId);
            if (!canView) return Forbid();
            // Assuming OrderViewModel is correctly defined and used
            var orderViewModel = new OrderViewModel { /* map properties */ Id = order.Id, ClientName = order.Client?.Name, OrderDate = order.OrderDate, Status = order.Status, SalesRepresentativeDisplay = order.SalesRepresentative?.UserName };
            ViewBag.OrderItems = order.OrderItems; // Or map to ViewModel property
            return View(orderViewModel);
        }

        private bool OrderExists(int id) { return _context.Orders.Any(e => e.Id == id); }

        private async Task RepopulateViewModelForCreateError(CreateOrderViewModel viewModel) {
            var salesRepUsers = await _userManager.GetUsersInRoleAsync("SalesRepresentative");
            var salesRepSelectListItems = salesRepUsers.OrderBy(u => u.UserName).Select(user => new SelectListItem { Value = user.Id.ToString(), Text = $"{user.UserName} ({(_userManager.GetRolesAsync(user).Result.FirstOrDefault() ?? "Сотрудник")})" }).ToList();
            var products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            viewModel.Clients = new SelectList(await _context.Clients.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", viewModel.ClientId);
            viewModel.SalesRepresentatives = new SelectList(salesRepSelectListItems, "Value", "Text", viewModel.SalesRepresentativeId);
            viewModel.ProductList = new SelectList(products, "Id", "Name");
            viewModel.ProductDetailsForJs = products.Select(p => new ProductInfoForJs(p.Id.ToString(), p.Name, p.Price)).ToList();
        }
        private async Task RepopulateViewModelForEditError(EditOrderViewModel viewModel) {
            // Similar to Create error repopulation
            var salesRepUsers = await _userManager.GetUsersInRoleAsync("SalesRepresentative");
            var salesRepSelectListItems = salesRepUsers.OrderBy(u => u.UserName).Select(user => new SelectListItem { Value = user.Id.ToString(), Text = $"{user.UserName} ({(_userManager.GetRolesAsync(user).Result.FirstOrDefault() ?? "Сотрудник")})" }).ToList();
            var products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            viewModel.Clients = new SelectList(await _context.Clients.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", viewModel.ClientId);
            viewModel.SalesRepresentatives = new SelectList(salesRepSelectListItems, "Value", "Text", viewModel.SalesRepresentativeId);
            viewModel.Statuses = new SelectList(Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString() }), "Value", "Text", viewModel.Status.ToString());
            viewModel.ProductList = new SelectList(products, "Id", "Name");
            viewModel.ProductDetailsForJs = products.Select(p => new ProductInfoForJs(p.Id.ToString(), p.Name, p.Price)).ToList();
        }
    }
}
