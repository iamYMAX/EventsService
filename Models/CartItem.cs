using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventsService.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CartId { get; set; }
        [ForeignKey("CartId")]
        public virtual Cart? Cart { get; set; } // Navigation property

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; } // Navigation property

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть не меньше 1.")]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Цена должна быть положительной.")]
        public decimal PricePerUnit { get; set; } // Price of the product at the time of adding to cart
    }
}
