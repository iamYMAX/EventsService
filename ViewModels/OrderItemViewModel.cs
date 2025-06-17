using System.ComponentModel.DataAnnotations;

namespace EventsService.ViewModels
{
    public class OrderItemViewModel
    {
        public int Id { get; set; } // Existing OrderItem ID, 0 for new
        public int ProductId { get; set; }

        [Display(Name = "Продукт/Услуга")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть не меньше 1.")]
        [Display(Name = "Количество")]
        public int Quantity { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть положительной.")]
        [Display(Name = "Цена за единицу")]
        public decimal PriceAtTimeOfOrder { get; set; } // Price per unit

        // Helper property for line total, not directly bound but useful for display
        public decimal LineTotal => Quantity * PriceAtTimeOfOrder;

        public bool IsMarkedForDeletion { get; set; } = false; // For handling deletions in POST
    }
}
