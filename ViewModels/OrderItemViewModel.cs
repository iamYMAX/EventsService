using System.ComponentModel.DataAnnotations;

namespace EventsService.ViewModels
{
    public class OrderItemViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        // Optional: To display product name or price in the form, not bound on POST but useful for display
        public string? ProductName { get; set; }
        public decimal ProductPrice { get; set; } // Price per unit
    }
}
