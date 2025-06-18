using EventsService.Data; // For ApplicationDbContext (though SessionCartService might encapsulate this)
using EventsService.Services; // For ISessionCartService
using EventsService.ViewModels; // For CartViewModel, CartItemViewModel
using Microsoft.AspNetCore.Mvc;
using System.Linq; // For Sum, Any
using System.Threading.Tasks;
using System.Collections.Generic; // For List
using System; // For DateTime in CartViewModel mapping

namespace EventsService.Controllers
{
    // No [Authorize] attribute at controller level yet, as anonymous users can use session cart.
    // Specific actions might get authorization later if needed (e.g., checkout).
    public class CartController : Controller
    {
        private readonly ISessionCartService _sessionCartService;
        // private readonly ICartService _dbCartService; // Will be added in Phase 2
        // private readonly UserManager<User> _userManager; // Will be added in Phase 2

        // Constructor for now only with ISessionCartService
        public CartController(ISessionCartService sessionCartService)
        {
            _sessionCartService = sessionCartService;
        }

        // GET: /Cart or /Cart/Index
        [HttpGet]
        public IActionResult Index() // Renamed from ViewCart for convention
        {
            var sessionCartItems = _sessionCartService.GetCartItems();

            // Map SessionCartItem to CartItemViewModel for consistency if needed,
            // or create a specific SessionCartViewModel.
            // For now, let's assume the View can handle List<SessionCartItem>
            // or we quickly adapt. The plan mentions a generic Views/Cart/Index.cshtml.
            // Let's prepare a simple ViewModel for it.

            var cartViewModel = new CartViewModel // Using the existing CartViewModel for structure
            {
                // Session cart doesn't have a persistent Id or ClientId in the same way
                // Id = 0,
                // ClientId = 0, // Or handle anonymous client representation
                LastModifiedDate = DateTime.UtcNow, // Or from session if stored
                Items = sessionCartItems.Select(sci => new CartItemViewModel
                {
                    // Id for CartItemViewModel could be ProductId for session cart if no other DB ID
                    Id = sci.ProductId, // Using ProductId as a stand-in for CartItem's own Id in this ViewModel context
                    ProductId = sci.ProductId,
                    ProductName = sci.ProductName,
                    Quantity = sci.Quantity,
                    PricePerUnit = sci.PricePerUnit
                }).ToList()
            };
            // GrandTotal is calculated by CartViewModel

            return View(cartViewModel);
        }

        // POST: /Cart/AddItemToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItemToCart(int productId, int quantity = 1)
        {
            if (quantity <= 0)
            {
                // Handle invalid quantity, perhaps with TempData message
                TempData["CartErrorMessage"] = "Количество должно быть положительным.";
                // Redirect to where the add action was initiated, or product details page
                // This requires knowing the return URL or having a robust way to redirect.
                // For now, redirecting to home/catalog.
                return RedirectToAction("Index", "Home");
            }

            await _sessionCartService.AddItemAsync(productId, quantity);
            TempData["CartSuccessMessage"] = "Товар добавлен в корзину."; // Feedback to user

            // Redirect to cart page to show updated cart
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/UpdateCartItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCartItem(int productId, int quantity)
        {
            if (quantity < 0) // Allow 0 to effectively remove, or handle as error
            {
                TempData["CartErrorMessage"] = "Количество не может быть отрицательным.";
                return RedirectToAction(nameof(Index));
            }

            if (quantity == 0) {
                 _sessionCartService.RemoveItem(productId);
                 TempData["CartSuccessMessage"] = "Товар удален из корзины.";
            } else {
                _sessionCartService.UpdateItemQuantity(productId, quantity);
                TempData["CartSuccessMessage"] = "Количество товара обновлено.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/RemoveCartItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveCartItem(int productId)
        {
            _sessionCartService.RemoveItem(productId);
            TempData["CartSuccessMessage"] = "Товар удален из корзины.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/ClearCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCart()
        {
            _sessionCartService.ClearCart();
            TempData["CartSuccessMessage"] = "Корзина очищена.";
            return RedirectToAction(nameof(Index));
        }
    }
}
