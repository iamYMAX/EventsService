using EventsService.Data;
using EventsService.Models;
using EventsService.ViewModels; // For CartViewModel, CartItemViewModel (will be created next)
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic; // Required for List
using System.Linq;
using System.Threading.Tasks;

namespace EventsService.Services
{
    public interface ICartService
    {
        Task<Cart> GetOrCreateCartAsync(int clientId); // Returns Cart entity
        Task AddItemToCartAsync(int clientId, int productId, int quantity);
        Task UpdateItemQuantityAsync(int clientId, int cartItemId, int newQuantity); // clientId for auth/cart ownership check
        Task RemoveItemFromCartAsync(int clientId, int cartItemId); // clientId for auth/cart ownership check
        Task ClearCartAsync(int clientId);
        Task<CartViewModel?> GetCartViewModelAsync(int clientId); // Nullable if cart somehow not found for client
    }

    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Cart> GetOrCreateCartAsync(int clientId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.ClientId == clientId);

            if (cart == null)
            {
                // Validate if ClientId exists before creating a cart for them
                var clientExists = await _context.Clients.AnyAsync(cl => cl.Id == clientId);
                if (!clientExists)
                {
                    // Or throw custom exception, or handle as per application's error strategy
                    throw new InvalidOperationException($"Client with ID {clientId} not found. Cannot create cart.");
                }

                cart = new Cart { ClientId = clientId, LastModifiedDate = DateTime.UtcNow };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }
            return cart;
        }

        public async Task AddItemToCartAsync(int clientId, int productId, int quantity)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

            var cart = await GetOrCreateCartAsync(clientId);
            var product = await _context.Products.FindAsync(productId);

            if (product == null) throw new InvalidOperationException($"Product with ID {productId} not found.");
            // Potentially check product availability/stock here if relevant

            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (cartItem == null) // New item
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    PricePerUnit = product.Price // Current product price
                });
            }
            else // Existing item, update quantity
            {
                cartItem.Quantity += quantity;
            }
            cart.LastModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateItemQuantityAsync(int clientId, int cartItemId, int newQuantity)
        {
            if (newQuantity <= 0)
            {
                await RemoveItemFromCartAsync(clientId, cartItemId); // Treat quantity <= 0 as removal
                return;
            }

            var cart = await GetOrCreateCartAsync(clientId); // Ensures we are operating on the correct client's cart
            var cartItem = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

            if (cartItem != null)
            {
                cartItem.Quantity = newQuantity;
                cart.LastModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            // Else: Item not found in this client's cart, could throw or ignore.
            // For now, it ignores if item not found in *this specific client's cart*.
        }

        public async Task RemoveItemFromCartAsync(int clientId, int cartItemId)
        {
            var cart = await GetOrCreateCartAsync(clientId);
            var cartItem = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem); // Or cart.Items.Remove(cartItem) and let EF handle it
                cart.LastModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(int clientId)
        {
            var cart = await GetOrCreateCartAsync(clientId);
            if (cart.Items.Any())
            {
                // _context.CartItems.RemoveRange(cart.Items); // More direct way if items are tracked by context
                cart.Items.Clear(); // EF Core should handle removal of dependent entities
                cart.LastModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<CartViewModel?> GetCartViewModelAsync(int clientId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(ci => ci.Product) // Include Product for ProductName, etc.
                .AsNoTracking() // Good for read-only operations
                .FirstOrDefaultAsync(c => c.ClientId == clientId);

            if (cart == null)
            {
                // If GetOrCreateCartAsync is standard before viewing, this might not be hit often.
                // If it can be hit, returning an empty VM might be better than null for some UIs.
                // For now, consistent with GetOrCreateCartAsync not being called by this specific method.
                // Consider creating an empty cart AND viewmodel if a client has none.
                // For now, if no cart record, return null. Client UI will need to handle.
                // Alternative: Call GetOrCreateCartAsync here to ensure a cart always exists.
                // This depends on desired app flow. Let's assume for now that a cart might not exist.
                return null;
            }

            // Placeholder for actual CartViewModel mapping (Step 5 of plan)
            // This will be created in the next step.
            // For this subtask, we'll assume CartViewModel and CartItemViewModel exist
            // in EventsService.ViewModels and do a basic mapping.
            var cartViewModel = new CartViewModel
            {
                Id = cart.Id,
                ClientId = cart.ClientId,
                LastModifiedDate = cart.LastModifiedDate,
                Items = cart.Items.Select(ci => new CartItemViewModel
                {
                    Id = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product?.Name ?? "Продукт не найден", // Product can be null if data issue
                    Quantity = ci.Quantity,
                    PricePerUnit = ci.PricePerUnit
                    // LineItemTotal is a calculated property in CartItemViewModel
                }).ToList()
                // GrandTotal is a calculated property in CartViewModel
            };
            return cartViewModel;
        }
    }
}
