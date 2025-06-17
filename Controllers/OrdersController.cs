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
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")]
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

            var productsForJs = await _context.Products
                                          .OrderBy(p => p.Name)
                                          .Select(p => new ProductInfoForJs(p.Id.ToString(), p.Name, p.Price))
                                          .ToListAsync();

            var viewModel = new CreateOrderViewModel
            {
                Clients = new SelectList(await _context.Clients.OrderBy(c => c.Name).ToListAsync(), "Id", "Name"),
                SalesRepresentatives = new SelectList(salesRepSelectListItems, "Value", "Text"),
                ProductList = new SelectList(productsForJs, "Value", "Text"), // Use Value and Text from ProductInfoForJs
                ProductDetailsForJs = productsForJs, // Populate the new list for JS
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.New,
                OrderItems = new List<OrderItemViewModel>() // Initialize for the view
            };
            return View(viewModel);
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")]
        public async Task<IActionResult> Create(CreateOrderViewModel viewModel)
        {
            // Manually check MinLength for OrderItems because it might not be hit if the list is null or empty initially
            // and client-side validation might be bypassed or not perfectly configured.
            if (viewModel.OrderItems == null || !viewModel.OrderItems.Any())
            {
                ModelState.AddModelError("OrderItems", "Please add at least one product to the order.");
            }

            if (ModelState.IsValid)
            {
                var order = new Order
                {
                    ClientId = viewModel.ClientId,
                    // SalesRepresentativeId will be set below based on user role
                    OrderDate = viewModel.OrderDate,
                    Status = viewModel.Status,
                    OrderItems = new List<OrderItem>() // Initialize the actual order's items collection
                };

                var currentUser = await _userManager.GetUserAsync(User); // Get current user
                if (currentUser == null)
                {
                    // This should ideally not happen due to [Authorize]
                    ModelState.AddModelError("", "Unable to identify current user.");
                    await RepopulateViewModelForCreateError(viewModel);
                    return View(viewModel);
                }

                if (User.IsInRole("SalesRepresentative"))
                {
                    order.SalesRepresentativeId = currentUser.Id;
                }
                else // For Admin/Manager or other roles that might be allowed to select
                {
                    order.SalesRepresentativeId = viewModel.SalesRepresentativeId;
                }


                if (viewModel.OrderItems != null && viewModel.OrderItems.Any()) // Redundant check if MinLength works, but good for safety
                {
                    foreach (var itemVM in viewModel.OrderItems)
                    {
                        var product = await _context.Products.FindAsync(itemVM.ProductId);
                        if (product != null)
                        {
                            // Check stock if necessary - for future enhancement
                            // if (product.StockQuantity < itemVM.Quantity)
                            // {
                            //     ModelState.AddModelError("", $"Not enough stock for {product.Name}. Available: {product.StockQuantity}, Requested: {itemVM.Quantity}");
                            //     continue; // Or break, depending on desired behavior
                            // }

                            order.OrderItems.Add(new OrderItem
                            {
                                ProductId = product.Id,
                                Quantity = itemVM.Quantity,
                                PriceAtTimeOfOrder = product.Price // Store price at time of order
                            });
                        }
                        else
                        {
                            ModelState.AddModelError("", $"Product with ID {itemVM.ProductId} not found. Please remove it or select a valid product.");
                            // No need to break, collect all such errors
                        }
                    }
                }
                // else: The MinLength attribute on OrderItems should handle the case where it's empty.
                // If it's null (e.g. form submission error or tampering), the initial check handles it.

                if (!ModelState.IsValid) // Check if any product not found errors or stock errors were added
                {
                    // Re-populate necessary dropdowns/data for the view if ModelState became invalid
                    await RepopulateViewModelForCreateError(viewModel);
                    return View(viewModel);
                }

                _context.Add(order);
                await _context.SaveChangesAsync();
                // TempData["SuccessMessage"] = "Заявка успешно создана!";
                return RedirectToAction(nameof(Index));
            }

            // If model state was initially invalid (before custom logic) or became invalid due to product issues
            await RepopulateViewModelForCreateError(viewModel);
            return View(viewModel);
        }

        private async Task RepopulateViewModelForCreateError(CreateOrderViewModel viewModel)
        {
            var productsForJs = await _context.Products
                                          .OrderBy(p => p.Name)
                                          .Select(p => new ProductInfoForJs(p.Id.ToString(), p.Name, p.Price))
                                          .ToListAsync();
            viewModel.ProductList = new SelectList(productsForJs, "Value", "Text");
            viewModel.ProductDetailsForJs = productsForJs;

            viewModel.Clients = new SelectList(await _context.Clients.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", viewModel.ClientId);

            var salesRepUsersForRepopulate = await _userManager.GetUsersInRoleAsync("SalesRepresentative");
            // Consider including other roles like "Manager" if they can also be sales reps
            // var managerUsers = await _userManager.GetUsersInRoleAsync("Manager");
            // var allPotentialReps = salesRepUsersForRepopulate.Concat(managerUsers).DistinctBy(u => u.Id);

            var salesRepSelectListItemsForRepopulate = new List<SelectListItem>();
            foreach (var userInRole in salesRepUsersForRepopulate.OrderBy(u => u.UserName)) // Adjust if using allPotentialReps
            {
                var roles = await _userManager.GetRolesAsync(userInRole);
                var primaryRole = roles.FirstOrDefault() ?? "Сотрудник";
                salesRepSelectListItemsForRepopulate.Add(new SelectListItem
                {
                    Value = userInRole.Id.ToString(),
                    Text = $"{userInRole.UserName} ({primaryRole})"
                });
            }
            viewModel.SalesRepresentatives = new SelectList(salesRepSelectListItemsForRepopulate, "Value", "Text", viewModel.SalesRepresentativeId);
            // Note: viewModel.OrderItems will retain its submitted values, which is good for correcting them.
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

        // GET: Orders/Edit/5
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")] // Adjust roles as needed
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.SalesRepresentative)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Authorization Check: Ensure user can edit this order
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge(); // Should not happen if [Authorize] is effective

            bool canEdit = false;
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                canEdit = true;
            }
            else if (User.IsInRole("SalesRepresentative") && order.SalesRepresentativeId == currentUser.Id)
            {
                canEdit = true;
            }

            if (!canEdit)
            {
                // Consider a more user-friendly "Access Denied" page or message
                // For now, Forbid() is clear for developers.
                return Forbid();
            }

            var viewModel = new EditOrderViewModel
            {
                Id = order.Id,
                ClientId = order.ClientId,
                SalesRepresentativeId = order.SalesRepresentativeId, // Already string? due to model change
                OrderDate = order.OrderDate,
                Status = order.Status,
                OrderItems = order.OrderItems.Select(oi => new OrderItemViewModel
                {
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    ProductName = oi.Product?.Name, // Product might be null if data integrity issue
                    ProductPrice = oi.PriceAtTimeOfOrder // Or oi.Product.Price if you want current price
                }).ToList()
            };

            // Populate SelectLists and ProductDetailsForJs for the ViewModel
            viewModel.Clients = new SelectList(await _context.Clients.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", viewModel.ClientId);

            var salesRepUsers = await _userManager.GetUsersInRoleAsync("SalesRepresentative");
            var salesRepSelectListItems = new List<SelectListItem>();
            foreach (var user in salesRepUsers.OrderBy(u => u.UserName))
            {
                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? "Сотрудник";
                salesRepSelectListItems.Add(new SelectListItem
                {
                    Value = user.Id.ToString(), // User ID is string
                    Text = $"{user.UserName} ({primaryRole})"
                });
            }
            viewModel.SalesRepresentatives = new SelectList(salesRepSelectListItems, "Value", "Text", viewModel.SalesRepresentativeId);

            viewModel.Statuses = new SelectList(Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString() }), "Value", "Text", viewModel.Status.ToString());

            var products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            // For ProductList SelectList, we need Value and Text. ProductInfoForJs has these.
            var productsForJs = products.Select(p => new ProductInfoForJs(p.Id.ToString(), p.Name, p.Price)).ToList();
            viewModel.ProductList = new SelectList(productsForJs, "Value", "Text");
            viewModel.ProductDetailsForJs = productsForJs;

            return View(viewModel); // View "Edit.cshtml" will be created next
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")] // Ensure roles match GET
        public async Task<IActionResult> Edit(int id, EditOrderViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound(); // Or BadRequest()
            }

            // Manually check MinLength for OrderItems if client-side validation might be bypassed
            if (viewModel.OrderItems == null || !viewModel.OrderItems.Any())
            {
                ModelState.AddModelError("OrderItems", "Please add at least one product to the order.");
            }

            if (!ModelState.IsValid)
            {
                // If model state is invalid, re-populate dropdowns and return view
                await RepopulateViewModelForEditError(viewModel);
                return View(viewModel);
            }

            var orderToUpdate = await _context.Orders
                .Include(o => o.OrderItems) // Include existing order items
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orderToUpdate == null)
            {
                return NotFound();
            }

            // Authorization Check (similar to GET)
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            bool canEdit = false;
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                canEdit = true;
            }
            else if (User.IsInRole("SalesRepresentative") && orderToUpdate.SalesRepresentativeId == currentUser.Id)
            {
                canEdit = true;
            }

            if (!canEdit)
            {
                return Forbid();
            }

            // Update scalar properties of the order
            orderToUpdate.ClientId = viewModel.ClientId;
            orderToUpdate.OrderDate = viewModel.OrderDate;
            orderToUpdate.Status = viewModel.Status;

            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                orderToUpdate.SalesRepresentativeId = viewModel.SalesRepresentativeId;
            }
            // SalesRep cannot change assignment via this Edit action. Their ID is preserved if they are editing.

            // Update OrderItems: Strategy - Remove existing and add new ones from ViewModel
            _context.OrderItems.RemoveRange(orderToUpdate.OrderItems); // Clear existing items for this order
            orderToUpdate.OrderItems = new List<OrderItem>(); // Initialize new collection

            if (viewModel.OrderItems != null && viewModel.OrderItems.Any())
            {
                foreach (var itemVM in viewModel.OrderItems)
                {
                    var product = await _context.Products.FindAsync(itemVM.ProductId);
                    if (product != null)
                    {
                        orderToUpdate.OrderItems.Add(new OrderItem
                        {
                            OrderId = orderToUpdate.Id, // Ensure OrderId is set (though EF might handle it)
                            ProductId = product.Id,
                            Quantity = itemVM.Quantity,
                            PriceAtTimeOfOrder = product.Price // Consider if price can change or if original price was stored
                        });
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Product with ID {itemVM.ProductId} not found. Please correct the order items.");
                    }
                }
            }

            if (!ModelState.IsValid) // Re-check model state after processing items
            {
                await RepopulateViewModelForEditError(viewModel);
                return View(viewModel);
            }

            try
            {
                //_context.Update(orderToUpdate); // Not always necessary if tracking is on
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(orderToUpdate.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Details), new { id = orderToUpdate.Id });
        }

        // Helper method to check if Order exists
        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }

        // Helper method to repopulate ViewModel data on POST error
        private async Task RepopulateViewModelForEditError(EditOrderViewModel viewModel)
        {
            viewModel.Clients = new SelectList(await _context.Clients.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", viewModel.ClientId);

            var salesRepUsers = await _userManager.GetUsersInRoleAsync("SalesRepresentative");
            var salesRepSelectListItems = new List<SelectListItem>();
            foreach (var user in salesRepUsers.OrderBy(u => u.UserName))
            {
                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? "Сотрудник";
                salesRepSelectListItems.Add(new SelectListItem { Value = user.Id.ToString(), Text = $"{user.UserName} ({primaryRole})" });
            }
            viewModel.SalesRepresentatives = new SelectList(salesRepSelectListItems, "Value", "Text", viewModel.SalesRepresentativeId);

            viewModel.Statuses = new SelectList(Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString() }), "Value", "Text", viewModel.Status.ToString());

            var products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            var productsForJs = products.Select(p => new ProductInfoForJs(p.Id.ToString(), p.Name, p.Price)).ToList(); // Corrected to ProductInfoForJs
            viewModel.ProductList = new SelectList(productsForJs, "Value", "Text");
            viewModel.ProductDetailsForJs = productsForJs;
        }
    }
}
