using EventsService.Data; // For ApplicationDbContext to get product details
using EventsService.Models; // For Product
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json; // For session serialization
using System.Threading.Tasks; // For async calls to DbContext if needed for product details

namespace EventsService.Services
{
    // Simple class to represent items in the session cart
    public class SessionCartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty; // Store for display
        public int Quantity { get; set; }
        public decimal PricePerUnit { get; set; } // Price at the time of adding
        public decimal LineTotal => Quantity * PricePerUnit;
    }

    public interface ISessionCartService
    {
        List<SessionCartItem> GetCartItems();
        Task AddItemAsync(int productId, int quantity);
        void UpdateItemQuantity(int productId, int quantity);
        void RemoveItem(int productId);
        void ClearCart();
        decimal GetGrandTotal();
        int GetCartItemCount();
    }

    public class SessionCartService : ISessionCartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _dbContext; // To fetch product details
        private const string CartSessionKey = "SessionCart";

        public SessionCartService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext dbContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _dbContext = dbContext;
        }

        private ISession? Session => _httpContextAccessor.HttpContext?.Session;

        private List<SessionCartItem> GetSessionCart()
        {
            var session = Session;
            if (session == null) return new List<SessionCartItem>();

            var cartJson = session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<SessionCartItem>();
            }
            try
            {
                return JsonSerializer.Deserialize<List<SessionCartItem>>(cartJson) ?? new List<SessionCartItem>();
            }
            catch
            {
                // Handle deserialization error, e.g., by clearing the invalid cart data
                session.Remove(CartSessionKey);
                return new List<SessionCartItem>();
            }
        }

        private void SaveSessionCart(List<SessionCartItem> cart)
        {
            var session = Session;
            if (session != null)
            {
                var cartJson = JsonSerializer.Serialize(cart);
                session.SetString(CartSessionKey, cartJson);
            }
        }

        public List<SessionCartItem> GetCartItems()
        {
            return GetSessionCart();
        }

        public async Task AddItemAsync(int productId, int quantity)
        {
            if (quantity <= 0) return; // Or throw exception

            var cart = GetSessionCart();
            var existingItem = cart.FirstOrDefault(item => item.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var product = await _dbContext.Products.FindAsync(productId);
                if (product != null)
                {
                    cart.Add(new SessionCartItem
                    {
                        ProductId = productId,
                        ProductName = product.Name,
                        Quantity = quantity,
                        PricePerUnit = product.Price // Current price from DB
                    });
                }
                // Else: product not found, perhaps log or throw error, or just don't add.
            }
            SaveSessionCart(cart);
        }

        public void UpdateItemQuantity(int productId, int quantity)
        {
            var cart = GetSessionCart();
            var itemToUpdate = cart.FirstOrDefault(item => item.ProductId == productId);

            if (itemToUpdate != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(itemToUpdate);
                }
                else
                {
                    itemToUpdate.Quantity = quantity;
                }
                SaveSessionCart(cart);
            }
        }

        public void RemoveItem(int productId)
        {
            var cart = GetSessionCart();
            var itemToRemove = cart.FirstOrDefault(item => item.ProductId == productId);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                SaveSessionCart(cart);
            }
        }

        public void ClearCart()
        {
            var session = Session;
            if (session != null)
            {
                session.Remove(CartSessionKey);
            }
        }

        public decimal GetGrandTotal()
        {
            var cart = GetSessionCart();
            return cart.Sum(item => item.LineTotal);
        }

        public int GetCartItemCount()
        {
            var cart = GetSessionCart();
            return cart.Sum(item => item.Quantity); // Sum of quantities of all items
            // Or if you mean distinct product lines: return cart.Count;
        }
    }
}
