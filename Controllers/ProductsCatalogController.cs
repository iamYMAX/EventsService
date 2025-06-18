using EventsService.Data;
using EventsService.Models;
using EventsService.Services; // For ICartService (will be used in Details POST)
using EventsService.Utilities; // Added for PaginatedList
// using EventsService.ViewModels; // May need ProductCatalogViewModel later
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // For UserManager to get current client
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // For List

namespace EventsService.Controllers
{
    [Authorize(Roles = "Client")] // Only authenticated clients can access the catalog
    public class ProductsCatalogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        // private readonly ICartService _cartService; // Will be needed for "Add to Cart" POST

        // Constructor updated when ICartService is used for Add to Cart
        public ProductsCatalogController(ApplicationDbContext context, UserManager<User> userManager /*, ICartService cartService */)
        {
            _context = context;
            _userManager = userManager;
            // _cartService = cartService;
        }

        // GET: ProductsCatalog
        public async Task<IActionResult> Index(int pageNumber = 1) // Basic pagination
        {
            int pageSize = 10; // Or from configuration
            var productsQuery = _context.Products
                                     .OrderBy(p => p.Name);

            // For now, pass entities directly. A ViewModel could be used for more display logic.
            var paginatedProducts = await PaginatedList<Product>.CreateAsync(productsQuery.AsNoTracking(), pageNumber, pageSize);
            return View(paginatedProducts);
        }

        // GET: ProductsCatalog/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Parameters)
                .Include(p => p.Characteristics)
                .Include(p => p.Properties)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // For now, pass entity directly. A ProductDetailViewModel could be used.
            return View(product);
        }

        // POST: ProductsCatalog/AddToCart (Example - this would likely be in CartController or handled by CartService via API)
        // For now, the "Add to Cart" button on Details page will be a form POSTing to CartController.
        // So, no POST action here yet. The "Add to Cart" button on Details view will point to CartController/AddItem.
    }
}
