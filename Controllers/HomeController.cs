using EventsService.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using EventsService.Data; // Added
using Microsoft.EntityFrameworkCore; // Added
using System.Linq; // Added
using System.Threading.Tasks; // Added
using EventsService.Utilities; // Added for PaginatedList

namespace EventsService.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // Add ApplicationDbContext

        // Modify constructor
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context; // Assign injected context
        }

        public async Task<IActionResult> Index(int pageNumber = 1) // Basic pagination
        {
            int pageSize = 9; // e.g., 9 products for a 3-column layout
            var productsQuery = _context.Products
                                     .OrderBy(p => p.Name); // Order by name by default

            var paginatedProducts = await PaginatedList<Product>.CreateAsync(productsQuery.AsNoTracking(), pageNumber, pageSize);

            return View(paginatedProducts);
        }

        // GET: Home/ProductDetails/5
        public async Task<IActionResult> ProductDetails(int? id)
        {
            if (id == null)
            {
                return NotFound(); // Or redirect to Index with an error message
            }

            var product = await _context.Products
                .Include(p => p.Parameters)
                .Include(p => p.Characteristics)
                .Include(p => p.Properties)
                .AsNoTracking() // Good for read-only detail views
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound(); // Product not found
            }

            return View(product); // Passes the Product model to Views/Home/ProductDetails.cshtml
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
