using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
// using EventsService.Models; // For Parameter, Characteristic, Property if needed

namespace EventsService.ViewModels
{
    public class EditProductViewModel
    {
        public int Id { get; set; } // Product Id

        [Required(ErrorMessage = "Наименование обязательно для заполнения.")]
        [StringLength(100, ErrorMessage = "Наименование не может превышать 100 символов.")]
        [Display(Name = "Наименование")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Описание не может превышать 500 символов.")]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Цена обязательна для заполнения.")]
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

        [Display(Name = "Загрузить новые изображения")]
        public IFormFileCollection? UploadedImages { get; set; } // For new uploads

        public List<ProductImageViewModel> ExistingImages { get; set; } = new List<ProductImageViewModel>();

        [Display(Name = "Основное изображение")]
        public int? PrimaryImageId { get; set; } // To set a new primary image from existing ones

        // Parameters, Characteristics, Properties are assumed to be loaded with the Product entity
        // and displayed directly in the Edit view, not part of this ViewModel's direct editable fields for now.
    }
}
