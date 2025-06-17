using EventsService.Data;
using EventsService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;

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
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,StockQuantity,PackagingDetails")] Product product, IFormFile? productImage)
        {
            if (ModelState.IsValid)
            {
                if (productImage != null && productImage.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(productImage.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("productImage", "Invalid image file type. Allowed types: .jpg, .jpeg, .png, .gif");
                        return View(product);
                    }

                    // Max file size (e.g., 5MB) - optional
                    // if (productImage.Length > 5 * 1024 * 1024)
                    // {
                    //     ModelState.AddModelError("productImage", "Image file size exceeds the limit (5MB).");
                    //     return View(product);
                    // }

                    var uploadsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/products");
                    if (!Directory.Exists(uploadsFolderPath))
                    {
                        Directory.CreateDirectory(uploadsFolderPath);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(productImage.FileName);
                    var filePath = Path.Combine(uploadsFolderPath, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await productImage.CopyToAsync(stream);
                    }
                    product.ImageUrl = "/uploads/products/" + uniqueFileName;
                }

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

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,StockQuantity,PackagingDetails,ImageUrl")] Product product, IFormFile? productImage)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (productImage != null && productImage.Length > 0)
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var extension = Path.GetExtension(productImage.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("productImage", "Invalid image file type. Allowed types: .jpg, .jpeg, .png, .gif");
                            return View(product);
                        }

                        // Delete old image if it exists and a new one is uploaded
                        if (!string.IsNullOrEmpty(product.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        var uploadsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/products");
                        if (!Directory.Exists(uploadsFolderPath))
                        {
                            Directory.CreateDirectory(uploadsFolderPath);
                        }

                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(productImage.FileName);
                        var filePath = Path.Combine(uploadsFolderPath, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await productImage.CopyToAsync(stream);
                        }
                        product.ImageUrl = "/uploads/products/" + uniqueFileName;
                    }
                    // If no new image is uploaded, product.ImageUrl (bound from form) remains unchanged.

                    _context.Update(product);
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
            return View(product);
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
