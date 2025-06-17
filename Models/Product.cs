using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventsService.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; } = 0;

        [StringLength(250)]
        [Display(Name = "Packaging Details")]
        public string? PackagingDetails { get; set; }

        [StringLength(1024)]
        [Display(Name = "Image URL/Path")]
        public string? ImageUrl { get; set; }

        public virtual ICollection<OrderItem>? OrderItems { get; set; }
    }
}
