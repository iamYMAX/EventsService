using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventsService.Models
{
    public class ProductImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required]
        [StringLength(2048)] // Max path length
        [Display(Name = "Путь к изображению")]
        public string ImagePath { get; set; } = string.Empty; // Relative path, e.g., /uploads/products/image.jpg

        [StringLength(255)]
        [Display(Name = "Подпись")]
        public string? Caption { get; set; } // Alt text or title

        [Display(Name = "Основное изображение")]
        public bool IsPrimary { get; set; } = false;

        [Display(Name = "Порядок сортировки")]
        public int SortOrder { get; set; } = 0; // For ordering multiple images
    }
}
