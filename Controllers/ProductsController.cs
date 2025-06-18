using EventsService.Data;
using EventsService.Models;
using EventsService.ViewModels; // Added
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace EventsService.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.OrderBy(p => p.Name).ToListAsync());
        }

        // GET: Products/Details/5
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

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            var viewModel = new CreateProductViewModel();
            // Any default values for the viewModel can be set here if needed
            return View(viewModel); // New: returns View(viewModel)
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,Quantity,SKU")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                // TempData["SuccessMessage"] = "Товар/услуга успешно создан(а).";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Images) // Eagerly load existing images
                .Include(p => p.Parameters)     // Keep these if view uses them
                .Include(p => p.Characteristics) // Keep these
                .Include(p => p.Properties)      // Keep these
                .AsNoTracking() // Good for read-only scenarios like populating an edit form
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            var viewModel = new EditProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                SKU = product.SKU,
                ExistingImages = product.Images?.Select(img => new ProductImageViewModel
                {
                    Id = img.Id,
                    ImagePath = img.ImagePath, // Assuming ImagePath is the relative path for display
                    Caption = img.Caption,
                    IsPrimary = img.IsPrimary
                }).ToList() ?? new List<ProductImageViewModel>(),
                PrimaryImageId = product.Images?.FirstOrDefault(img => img.IsPrimary)?.Id
            };

            // If the view also needs access to Parameters, Characteristics, Properties,
            // and they are not part of EditProductViewModel, you might pass the original 'product'
            // entity via ViewBag, or reconsider adding them to EditProductViewModel if they are editable.
            // For now, the view will be typed to EditProductViewModel.
            // The existing Products/Edit.cshtml view might need updates if it directly accessed product.Parameters etc.
            // and now needs to get them from a ViewBag or if the ViewModel is the sole source.
            // Let's assume the view will be adapted for EditProductViewModel.

            return View(viewModel);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Quantity,SKU")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch the existing product from DB, including its collections
                    var productToUpdate = await _context.Products
                        .Include(p => p.Parameters)
                        .Include(p => p.Characteristics)
                        .Include(p => p.Properties)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (productToUpdate == null)
                    {
                        return NotFound();
                    }

                    // Update only the scalar properties from the bound 'product' model
                    productToUpdate.Name = product.Name;
                    productToUpdate.Description = product.Description;
                    productToUpdate.Price = product.Price;
                    productToUpdate.Quantity = product.Quantity;
                    productToUpdate.SKU = product.SKU;

                    // Note: Changes to Parameters, Characteristics, Properties are not handled here directly.
                    // This would require more complex logic to compare and update collections,
                    // often done with specific UI elements in the view (e.g., JavaScript based forms).

                    _context.Update(productToUpdate);
                    await _context.SaveChangesAsync();
                    // TempData["SuccessMessage"] = "Товар/услуга успешно обновлен(а).";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            // If ModelState is invalid, we need to reload the product with its collections for the view
            var productForView = await _context.Products
                .Include(p => p.Parameters)
                .Include(p => p.Characteristics)
                .Include(p => p.Properties)
                .FirstOrDefaultAsync(p => p.Id == id);
            return View(productForView); // Return the fully loaded product to the view
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // Before deleting, check if product is part of any OrderItem
                var isProductInOrder = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
                if (isProductInOrder)
                {
                    // TempData["ErrorMessage"] = "Невозможно удалить товар/услугу, так как он(а) используется в заявках. Сначала удалите его/ее из всех заявок или архивируйте товар.";
                    ModelState.AddModelError(string.Empty, "Невозможно удалить товар/услугу, так как он(а) используется в существующих заявках. Рассмотрите возможность архивации товара вместо удаления.");
                    return View(product); // Return to Delete view to show the error
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                // TempData["SuccessMessage"] = "Товар/услуга успешно удален(а).";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
