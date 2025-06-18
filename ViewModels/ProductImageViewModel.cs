using System.ComponentModel.DataAnnotations;

namespace EventsService.ViewModels
{
    public class ProductImageViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Путь к изображению")]
        public string ImagePath { get; set; } = string.Empty;

        [Display(Name = "Подпись")]
        public string? Caption { get; set; }

        [Display(Name = "Основное")]
        public bool IsPrimary { get; set; }

        public bool IsMarkedForDeletion { get; set; } // For Edit form
    }
}
