using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace EventsService.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Наименование обязательно для заполнения.")]
        [StringLength(100, ErrorMessage = "Наименование не может превышать 100 символов.")]
        [Display(Name = "Наименование")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Описание не может превышать 500 символов.")]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Цена обязательна для заполнения.")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть положительным числом.")]
        [Display(Name = "Цена")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Количество обязательно для заполнения.")]
        [Range(0, int.MaxValue, ErrorMessage = "Количество должно быть неотрицательным числом.")]
        [Display(Name = "Количество")]
        public int Quantity { get; set; }

        [StringLength(50, ErrorMessage = "Артикул (SKU) не может превышать 50 символов.")]
        [Display(Name = "Артикул (SKU)")]
        public string? SKU { get; set; }

        public virtual ICollection<Parameter>? Parameters { get; set; }
        public virtual ICollection<Characteristic>? Characteristics { get; set; }
        public virtual ICollection<Property>? Properties { get; set; }
        public virtual ICollection<OrderItem>? OrderItems { get; set; }
    }
}
