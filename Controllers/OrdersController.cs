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
                    Text = $"{((user.FirstName + " " + user.LastName).Trim() == "" ? user.UserName : (user.FirstName + " " + user.LastName).Trim())} ({primaryRole})"
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
                    Text = $"{((user.FirstName + " " + user.LastName).Trim() == "" ? user.UserName : (user.FirstName + " " + user.LastName).Trim())} ({primaryRole})"
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

        // GET: Orders/Edit/5
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")]
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
                .FirstOrDefaultAsync(o => o.Id == id.Value); // Ensure id has value

            if (order == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                // This should ideally not happen if [Authorize] is effective
                // and user is authenticated.
                return Challenge();
            }

            bool isAdmin = User.IsInRole("Admin");
            bool isManager = User.IsInRole("Manager");
            // Check if the current user is THE sales representative assigned to this order
            bool isAssignedSalesRepresentative = User.IsInRole("SalesRepresentative") && order.SalesRepresentativeId == currentUser.Id;

            // Authorization: Who can edit this specific order?
            if (!isAdmin && !isManager && !isAssignedSalesRepresentative)
            {
                return Forbid();
            }

            // Prepare Sales Rep Select List (for changing the Sales Rep)
            var allUsersForSalesRepDropdown = new List<User>();
            var salesRepRoleUsers = await _userManager.GetUsersInRoleAsync("SalesRepresentative");
            allUsersForSalesRepDropdown.AddRange(salesRepRoleUsers);
            // Optional: Add Managers to the list of potential Sales Representatives if they can be assigned
            // var managerRoleUsers = await _userManager.GetUsersInRoleAsync("Manager");
            // allUsersForSalesRepDropdown.AddRange(managerRoleUsers);
            // allUsersForSalesRepDropdown = allUsersForSalesRepDropdown.DistinctBy(u => u.Id).ToList();


            var salesRepSelectListItems = new List<SelectListItem>();
            foreach (var user in allUsersForSalesRepDropdown.OrderBy(u => u.UserName))
            {
                var roles = await _userManager.GetRolesAsync(user);
                string? primaryRole = roles.FirstOrDefault() ?? "Сотрудник"; // Default role text

                string namePart = (user.FirstName + " " + user.LastName).Trim();
                if (string.IsNullOrWhiteSpace(namePart))
                {
                    namePart = user.UserName; // Fallback to UserName
                }
                salesRepSelectListItems.Add(new SelectListItem
                {
                    Value = user.Id.ToString(),
                    Text = $"{namePart} ({primaryRole})"
                });
            }

            // Determine current SalesRep's display name for the view model
            string currentSalesRepDisplayName = "Не назначен";
            if (order.SalesRepresentative != null)
            {
                // Construct the name carefully to avoid issues with null FirstName/LastName before Trim
                string fName = order.SalesRepresentative.FirstName ?? "";
                string lName = order.SalesRepresentative.LastName ?? "";
                currentSalesRepDisplayName = (fName + " " + lName).Trim();

                if (string.IsNullOrWhiteSpace(currentSalesRepDisplayName))
                {
                    currentSalesRepDisplayName = order.SalesRepresentative.UserName ?? "N/A"; // Ensure UserName fallback is also safe
                }
            }

            var viewModel = new EditOrderViewModel
            {
                OrderId = order.Id,
                ClientId = order.ClientId,
                ClientName = order.Client?.Name ?? "N/A", // Display current client name
                SalesRepresentativeId = order.SalesRepresentativeId,
                SalesRepresentativeName = currentSalesRepDisplayName, // Display current sales rep name
                OrderDate = order.OrderDate,
                ScheduledServiceDateTime = order.ScheduledServiceDateTime, // Add this line
                Status = order.Status,
                OrderItems = order.OrderItems?.Select(oi => new OrderItemViewModel
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? "N/A",
                    Quantity = oi.Quantity,
                    PriceAtTimeOfOrder = oi.PriceAtTimeOfOrder
                }).ToList(),

                // SelectLists for dropdowns
                Clients = new SelectList(await _context.Clients.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", order.ClientId),
                SalesRepresentatives = new SelectList(salesRepSelectListItems, "Value", "Text", order.SalesRepresentativeId),
                Statuses = new SelectList(
                    Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>()
                        .Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString() /* TODO: Localize enum names */ })
                        .ToList(),
                    "Value", "Text", order.Status.ToString()),
                AvailableProducts = new MultiSelectList(await _context.Products.OrderBy(p => p.Name).ToListAsync(), "Id", "Name"),
                SelectedProductIds = new List<int>(), // For adding new products, initially empty

                // Role flags for the current logged-in user
                IsAdmin = isAdmin,
                IsManager = isManager,
                IsSalesRepresentative = isAssignedSalesRepresentative, // True only if current user is THE assigned SalesRep

                // Detailed permission flags based on roles
                CanEditClient = isAdmin,
                CanEditSalesRepresentative = isAdmin || isManager,
                CanEditOrderDate = isAdmin,
                CanEditStatus = isAdmin || isManager || isAssignedSalesRepresentative,
                CanEditOrderItems = isAdmin || isManager || isAssignedSalesRepresentative,
                CanAddOrderItems = isAdmin || isManager || isAssignedSalesRepresentative,
                CanDeleteOrderItems = isAdmin || isManager || isAssignedSalesRepresentative
            };

            return View("Edit", viewModel);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,SalesRepresentative")]
        public async Task<IActionResult> Edit(int id, EditOrderViewModel viewModel)
        {
            if (id != viewModel.OrderId)
            {
                return NotFound(); // Or BadRequest()
            }

            var orderToUpdate = await _context.Orders
                .Include(o => o.OrderItems) // Crucial for updating items
                .Include(o => o.Client) // For validation/display if needed
                .Include(o => o.SalesRepresentative) // For validation/display
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orderToUpdate == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            bool isAdmin = User.IsInRole("Admin");
            bool isManager = User.IsInRole("Manager");
            bool isAssignedSalesRepresentative = User.IsInRole("SalesRepresentative") && orderToUpdate.SalesRepresentativeId == currentUser.Id;

            // Server-side authorization check for who can edit this order
            if (!isAdmin && !isManager && !isAssignedSalesRepresentative)
            {
                return Forbid(); // User not allowed to edit this order at all
            }

            // Re-evaluate permissions based on the actual user (not just flags from GET)
            bool canEditClient = isAdmin;
            bool canEditSalesRepresentative = isAdmin || isManager;
            bool canEditOrderDate = isAdmin;
            bool canEditStatus = isAdmin || isManager || isAssignedSalesRepresentative;
            bool canEditOrderItems = isAdmin || isManager || isAssignedSalesRepresentative; // Modify existing items
            bool canAddOrderItems = isAdmin || isManager || isAssignedSalesRepresentative;
            bool canDeleteOrderItems = isAdmin || isManager || isAssignedSalesRepresentative;


            if (ModelState.IsValid)
            {
                // Update scalar properties based on permissions
                if (canEditClient) orderToUpdate.ClientId = viewModel.ClientId;
                if (canEditSalesRepresentative) orderToUpdate.SalesRepresentativeId = viewModel.SalesRepresentativeId; // Nullable

                if (canEditOrderDate) // This flag is currently true only for Admins
                {
                   orderToUpdate.OrderDate = viewModel.OrderDate;
                   orderToUpdate.ScheduledServiceDateTime = viewModel.ScheduledServiceDateTime; // Add this line
                }

                if (canEditStatus) orderToUpdate.Status = viewModel.Status;

                // OrderItems processing
                if (canEditOrderItems || canAddOrderItems || canDeleteOrderItems)
                {
                    // 1. Handle existing items (update quantity, mark for deletion)
                    if (viewModel.OrderItems != null && orderToUpdate.OrderItems != null) // Added null check for orderToUpdate.OrderItems
                    {
                        foreach (var itemVM in viewModel.OrderItems)
                        {
                            var existingItem = orderToUpdate.OrderItems.FirstOrDefault(oi => oi.Id == itemVM.Id);
                            if (existingItem != null)
                            {
                                if (itemVM.IsMarkedForDeletion && canDeleteOrderItems)
                                {
                                    _context.OrderItems.Remove(existingItem);
                                }
                                else if (canEditOrderItems) // Can only update qty/price if allowed to edit items
                                {
                                    existingItem.Quantity = itemVM.Quantity;
                                    // PriceAtTimeOfOrder for existing items is generally not changed
                                    // unless specifically allowed and handled.
                                    // existingItem.PriceAtTimeOfOrder = itemVM.PriceAtTimeOfOrder;
                                }
                            }
                        }
                    }

                    // 2. Handle newly added products
                    if (viewModel.SelectedProductIds != null && viewModel.SelectedProductIds.Any() && canAddOrderItems)
                    {
                        orderToUpdate.OrderItems ??= new List<OrderItem>(); // Ensure collection is initialized
                        foreach (var productId in viewModel.SelectedProductIds)
                        {
                            var product = await _context.Products.FindAsync(productId);
                            if (product != null)
                            {
                                // Check if this product (as a new item) is already in the order to avoid duplicates,
                                // or if the business logic allows adding same product multiple times as separate line items.
                                // For simplicity, we add it as a new line item here.
                                var newOrderItem = new OrderItem
                                {
                                    OrderId = orderToUpdate.Id,
                                    ProductId = productId,
                                    Quantity = 1, // Default quantity for new items, can be adjusted
                                    PriceAtTimeOfOrder = product.Price // Get current price
                                };
                                orderToUpdate.OrderItems.Add(newOrderItem);
                            }
                        }
                    }
                } // End OrderItems processing block

                try
                {
                    await _context.SaveChangesAsync();
                    // TempData["SuccessMessage"] = "Заявка успешно обновлена!";
                    return RedirectToAction(nameof(Details), new { id = orderToUpdate.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Orders.Any(e => e.Id == orderToUpdate.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        // Log the error, add a model error, and return to the view
                        ModelState.AddModelError(string.Empty, "Не удалось сохранить изменения. Заявка была изменена другим пользователем. Пожалуйста, обновите страницу и попробуйте снова.");
                        // Potentially reload orderToUpdate from DB to show latest values if concurrency is an issue
                    }
                }
                catch (Exception ex)
                {
                     ModelState.AddModelError(string.Empty, $"Произошла ошибка при сохранении: {ex.Message}");
                }
            } // End ModelState.IsValid

            // If we got this far, something failed, re-populate necessary data for the view
            // This is similar to the GET action's population logic

            // Re-populate Sales Rep Select List
            var allUsersForSalesRepDropdown = new List<User>();
            var salesRepRoleUsers = await _userManager.GetUsersInRoleAsync("SalesRepresentative");
            allUsersForSalesRepDropdown.AddRange(salesRepRoleUsers);
            // Omitting managers from dropdown for now unless explicitly requested for POST error path

            var salesRepSelectListItems = new List<SelectListItem>();
            foreach (var user in allUsersForSalesRepDropdown.OrderBy(u => u.UserName))
            {
                var roles = await _userManager.GetRolesAsync(user);
                string? primaryRole = roles.FirstOrDefault() ?? "Сотрудник"; // Made primaryRole nullable
                string namePart = (user.FirstName + " " + user.LastName).Trim();
                if (string.IsNullOrWhiteSpace(namePart)) namePart = user.UserName;
                salesRepSelectListItems.Add(new SelectListItem { Value = user.Id.ToString(), Text = $"{namePart} ({primaryRole})" });
            }
            viewModel.SalesRepresentatives = new SelectList(salesRepSelectListItems, "Value", "Text", viewModel.SalesRepresentativeId);

            // Re-populate Clients Select List
            viewModel.Clients = new SelectList(await _context.Clients.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", viewModel.ClientId);

            // Re-populate Statuses Select List
            viewModel.Statuses = new SelectList(
                Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>()
                    .Select(e => new SelectListItem { Value = e.ToString(), Text = e.ToString() })
                    .ToList(),
                "Value", "Text", viewModel.Status.ToString());

            // Re-populate Available Products
            viewModel.AvailableProducts = new MultiSelectList(await _context.Products.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", viewModel.SelectedProductIds);

            // Re-set permission flags (as they are not part of the posted ViewModel)
            viewModel.IsAdmin = isAdmin;
            viewModel.IsManager = isManager;
            viewModel.IsSalesRepresentative = isAssignedSalesRepresentative;
            viewModel.CanEditClient = canEditClient;
            viewModel.CanEditSalesRepresentative = canEditSalesRepresentative;
            viewModel.CanEditOrderDate = canEditOrderDate;
            viewModel.CanEditStatus = canEditStatus;
            viewModel.CanEditOrderItems = canEditOrderItems;
            viewModel.CanAddOrderItems = canAddOrderItems;
            viewModel.CanDeleteOrderItems = canDeleteOrderItems;

            // Ensure OrderItems in ViewModel still reflects what was attempted or loaded if not valid
            // The binding might already handle OrderItems list, but if new items were conceptually added
            // before validation failed for another field, they might not be in orderToUpdate.OrderItems.
            // For robustness, one might rebuild viewModel.OrderItems based on orderToUpdate.OrderItems
            // and any uncommitted (but valid from VM) new items if the design gets more complex.
            // For now, the default model binding for viewModel.OrderItems is relied upon.
            if (viewModel.OrderItems == null || !viewModel.OrderItems.Any()) {
                viewModel.OrderItems = orderToUpdate.OrderItems?.Select(oi => new OrderItemViewModel
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = _context.Products.Find(oi.ProductId)?.Name ?? "N/A", // Re-fetch product name
                    Quantity = oi.Quantity,
                    PriceAtTimeOfOrder = oi.PriceAtTimeOfOrder
                }).ToList();
            }


            return View("Edit", viewModel);
        }
    }
}
