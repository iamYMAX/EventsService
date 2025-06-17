using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventsService.Models
{
    public class Property
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Наименование обязательно для заполнения.")]
        [StringLength(100, ErrorMessage = "Наименование не может превышать 100 символов.")]
        [Display(Name = "Наименование")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Значение обязательно для заполнения.")]
        [StringLength(255, ErrorMessage = "Значение не может превышать 255 символов.")]
        [Display(Name = "Значение")]
        public string Value { get; set; } = string.Empty;

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }
    }
}
