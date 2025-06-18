using EventsService.Data;
using EventsService.Models;
using EventsService.ViewModels; // Added
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting; // Added
using Microsoft.AspNetCore.Http; // Added for IFormFileCollection
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO; // Added for Path operations
using System.Linq;
using System.Threading.Tasks;

namespace EventsService.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // Added

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment) // Modified
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment; // Added
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
        public async Task<IActionResult> Create(CreateProductViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                Product product = new Product
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    Price = viewModel.Price,
                    Quantity = viewModel.Quantity,
                    SKU = viewModel.SKU
                    // Images collection will be populated after product is saved and has an Id
                };

                _context.Add(product);
                await _context.SaveChangesAsync(); // Save product to get its Id

                // Handle Image Uploads
                if (viewModel.UploadedImages != null && viewModel.UploadedImages.Any())
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "products");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    bool isFirstImage = true; // To set the first uploaded image as primary

                    foreach (var uploadedFile in viewModel.UploadedImages)
                    {
                        if (uploadedFile.Length > 0)
                        {
                            // Basic validation (example: file type and size)
                            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                            var extension = Path.GetExtension(uploadedFile.FileName).ToLowerInvariant();
                            long maxFileSize = 2 * 1024 * 1024; // 2MB

                            if (!allowedExtensions.Contains(extension))
                            {
                                ModelState.AddModelError("UploadedImages", $"Файл '{uploadedFile.FileName}' имеет недопустимое расширение.");
                                continue; // Skip this file
                            }
                            if (uploadedFile.Length > maxFileSize)
                            {
                                ModelState.AddModelError("UploadedImages", $"Файл '{uploadedFile.FileName}' превышает максимальный размер 2MB.");
                                continue; // Skip this file
                            }

                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(uploadedFile.FileName);
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            try
                            {
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await uploadedFile.CopyToAsync(fileStream);
                                }

                                var productImage = new ProductImage
                                {
                                    ProductId = product.Id,
                                    ImagePath = "/uploads/products/" + uniqueFileName, // Store relative path
                                    // Caption = "Default Caption", // Or get from ViewModel if field exists
                                    IsPrimary = isFirstImage, // Set first valid uploaded image as primary
                                    SortOrder = 0 // Basic sort order
                                };
                                _context.ProductImages.Add(productImage);
                                isFirstImage = false; // Only first image in this batch is primary
                            }
                            catch (Exception ex)
                            {
                                // Log error, add model error
                                ModelState.AddModelError("UploadedImages", $"Ошибка при загрузке файла '{uploadedFile.FileName}': {ex.Message}");
                                // Potentially delete already saved file if transactionality is critical
                            }
                        }
                    }

                    if (ModelState.IsValid) // Check if any errors were added during file processing
                    {
                        await _context.SaveChangesAsync(); // Save ProductImage entities
                    }
                    else
                    {
                        // If file errors occurred, we might need to decide if product creation should be rolled back
                        // or if it's acceptable to have a product created without its intended images.
                        // For now, the product is created, but we return to the view with errors.
                        // The view for Create does not currently show existing product details or images.
                        // This error path might need refinement if product creation should fail entirely.
                        // _context.Remove(product); // Example of rollback if needed
                        // await _context.SaveChangesAsync();
                        // return View(viewModel); // Return with file errors
                    }
                }

                if (ModelState.IsValid) // Final check before redirect
                {
                    // TempData["SuccessMessage"] = "Товар/услуга успешно создан(а).";
                    return RedirectToAction(nameof(Index));
                }
            }
            // If ModelState was initially invalid or became invalid due to image processing
            return View(viewModel);
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
        public async Task<IActionResult> Edit(int id, EditProductViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var productToUpdate = await _context.Products
                    .Include(p => p.Images) // Load existing images
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (productToUpdate == null)
                {
                    return NotFound();
                }

                // Map scalar properties from ViewModel to Entity
                productToUpdate.Name = viewModel.Name;
                productToUpdate.Description = viewModel.Description;
                productToUpdate.Price = viewModel.Price;
                productToUpdate.Quantity = viewModel.Quantity;
                productToUpdate.SKU = viewModel.SKU;

                // --- Start Image Processing ---

                // 1. Process existing images (deletions, find current primary)
                // int? currentPrimaryImageIdFromDb = productToUpdate.Images.FirstOrDefault(img => img.IsPrimary)?.Id; // Not strictly needed with new logic

                if (viewModel.ExistingImages != null)
                {
                    foreach (var imageVM in viewModel.ExistingImages)
                    {
                        var existingImage = productToUpdate.Images.FirstOrDefault(img => img.Id == imageVM.Id);
                        if (existingImage != null)
                        {
                            if (imageVM.IsMarkedForDeletion)
                            {
                                _context.ProductImages.Remove(existingImage);
                                // Delete physical file
                                if (!string.IsNullOrEmpty(existingImage.ImagePath))
                                {
                                    string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, existingImage.ImagePath.TrimStart('/'));
                                    if (System.IO.File.Exists(fullPath))
                                    {
                                        try { System.IO.File.Delete(fullPath); }
                                        catch (Exception ex) { /* Log error, but don't let it stop DB ops necessarily */
                                            ModelState.AddModelError("ExistingImages", $"Ошибка удаления файла '{existingImage.ImagePath}': {ex.Message}");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Update caption if it was editable and changed (ProductImageViewModel needs Caption editable for this)
                                // existingImage.Caption = imageVM.Caption;
                            }
                        }
                    }
                }

                // 2. Process new image uploads
                var newlyUploadedImages = new List<ProductImage>();
                if (viewModel.UploadedImages != null && viewModel.UploadedImages.Any())
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "products");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    foreach (var uploadedFile in viewModel.UploadedImages)
                    {
                        if (uploadedFile.Length > 0)
                        {
                            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                            var extension = Path.GetExtension(uploadedFile.FileName).ToLowerInvariant();
                            long maxFileSize = 2 * 1024 * 1024; // 2MB

                            if (!allowedExtensions.Contains(extension) || uploadedFile.Length > maxFileSize)
                            {
                                ModelState.AddModelError("UploadedImages", "Один или несколько файлов имеют неверный формат или размер.");
                                // Skip this file, continue with others
                                continue;
                            }

                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(uploadedFile.FileName);
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            try
                            {
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await uploadedFile.CopyToAsync(fileStream);
                                }
                                var newProductImage = new ProductImage
                                {
                                    // ProductId will be set when adding to productToUpdate.Images or explicitly
                                    ImagePath = "/uploads/products/" + uniqueFileName,
                                    // Caption = ..., // If caption for new uploads is supported
                                    IsPrimary = false, // Handled later
                                    SortOrder = 0 // Or implement logic for sort order
                                };
                                newlyUploadedImages.Add(newProductImage);
                            }
                            catch (Exception ex)
                            {
                                ModelState.AddModelError("UploadedImages", $"Ошибка загрузки файла '{uploadedFile.FileName}': {ex.Message}");
                            }
                        }
                    }
                    // Add newly uploaded images to the product's collection
                    // This assumes productToUpdate.Images is not null (it's initialized in Product model)
                    foreach(var newImg in newlyUploadedImages) {
                        productToUpdate.Images!.Add(newImg);
                    }
                }

                // 3. Set Primary Image (Revised Logic)
                ProductImage? imageToMakePrimary = null;

                if (viewModel.PrimaryImageId.HasValue && viewModel.PrimaryImageId.Value != 0)
                {
                    // User selected an existing image as primary
                    imageToMakePrimary = productToUpdate.Images.FirstOrDefault(i => i.Id == viewModel.PrimaryImageId.Value && _context.Entry(i).State != EntityState.Deleted);
                }
                else if (newlyUploadedImages.Any())
                {
                    // No existing image selected as primary OR user selected "none" (if that was an option)
                    // If new images were uploaded, make the first new one primary,
                    // but ONLY if no existing image (that's NOT being deleted) is already primary.
                    bool anExistingPrimaryRemains = productToUpdate.Images
                        .Any(i => i.IsPrimary && i.Id != 0 && // Check if it's an existing image
                                    !(viewModel.ExistingImages?.FirstOrDefault(eivm => eivm.Id == i.Id)?.IsMarkedForDeletion ?? false) && // And it's not marked for deletion
                                    i.Id != viewModel.PrimaryImageId); // And it's not the one currently selected (in case of re-selecting same primary)

                    if (!anExistingPrimaryRemains)
                    {
                       imageToMakePrimary = newlyUploadedImages.First();
                    } else if (productToUpdate.Images.Any(i => i.IsPrimary && i.Id == viewModel.PrimaryImageId.Value)) {
                        // If the selected primary is an existing one that was already primary, ensure it's set.
                        imageToMakePrimary = productToUpdate.Images.FirstOrDefault(i => i.Id == viewModel.PrimaryImageId.Value);
                    }
                }

                // Apply primary status
                var imagesToConsider = productToUpdate.Images.Where(img => _context.Entry(img).State != EntityState.Deleted).ToList();
                imagesToConsider.AddRange(newlyUploadedImages.Except(imagesToConsider));


                foreach (var img in imagesToConsider)
                {
                   img.IsPrimary = (imageToMakePrimary != null && img == imageToMakePrimary);
                }

                // Final fallback: if no image is primary and there are images, make the first one primary.
                if (!imagesToConsider.Any(i => i.IsPrimary) && imagesToConsider.Any())
                {
                    imagesToConsider.First().IsPrimary = true;
                }

                // --- End Image Processing ---

                if (!ModelState.IsValid) // Check for errors from image processing
                {
                    // Repopulate ExistingImages for the view model if returning due to error
                    viewModel.ExistingImages = productToUpdate.Images
                        .Where(img => _context.Entry(img).State != EntityState.Deleted)
                        .Select(img => new ProductImageViewModel {
                            Id = img.Id, ImagePath = img.ImagePath, Caption = img.Caption, IsPrimary = img.IsPrimary
                        }).ToList();
                     // Ensure PrimaryImageId is also correctly repopulated if it was part of the issue or needs to be sticky
                    viewModel.PrimaryImageId = productToUpdate.Images.FirstOrDefault(i => i.IsPrimary && _context.Entry(i).State != EntityState.Deleted)?.Id;
                    return View(viewModel);
                }

                try
                {
                    _context.Update(productToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(viewModel.Id)) { return NotFound(); }
                    else { throw; }
                }
                return RedirectToAction(nameof(Index));
            }

            // If ModelState was initially invalid, repopulate ExistingImages for the view model
            var productForView = await _context.Products.Include(p => p.Images).AsNoTracking().FirstOrDefaultAsync(p => p.Id == viewModel.Id);
            if (productForView != null) {
                viewModel.ExistingImages = productForView.Images?.Select(img => new ProductImageViewModel {
                    Id = img.Id, ImagePath = img.ImagePath, Caption = img.Caption, IsPrimary = img.IsPrimary
                }).ToList() ?? new List<ProductImageViewModel>();
                if (!viewModel.PrimaryImageId.HasValue) { // Pre-select primary if not already set by POST
                     viewModel.PrimaryImageId = productForView.Images?.FirstOrDefault(i => i.IsPrimary)?.Id;
                }
            } else { // Product not found, clear existing images if any were bound from a faulty GET
                 viewModel.ExistingImages.Clear();
            }
            return View(viewModel);
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
