using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EventsService.Models; // For Parameter, Characteristic, Property if handled directly

namespace EventsService.ViewModels
{
    public class CreateProductViewModel
    {
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

        // For image uploads
        [Display(Name = "Загрузить изображения")]
        public IFormFileCollection? UploadedImages { get; set; }

        // If Parameters, Characteristics, Properties are created at the same time:
        // public List<Parameter> Parameters { get; set; } = new List<Parameter>();
        // public List<Characteristic> Characteristics { get; set; } = new List<Characteristic>();
        // public List<Property> Properties { get; set; } = new List<Property>();
        // For simplicity, these are not included here yet; they are managed separately by current ProductsController.
        // If image upload is complex, these might be added via separate actions after product creation.
    }
}
