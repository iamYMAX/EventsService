using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventsService.Models
{
    public class ScheduledEvent
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Заголовок события обязателен.")]
        [StringLength(200, ErrorMessage = "Заголовок не может превышать 200 символов.")]
        [Display(Name = "Заголовок")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов.")]
        [Display(Name = "Описание")]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Время начала обязательно.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Время начала")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Время окончания обязательно.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Время окончания")]
        // TODO: Add custom validation for StartTime < EndTime if possible at model level,
        // otherwise ensure in ViewModel/Controller.
        public DateTime EndTime { get; set; }

        [Display(Name = "Ответственный сотрудник")]
        public int? UserId { get; set; } // Foreign Key for User (Sales Representative/Assigned Person)
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Display(Name = "Клиент")]
        public int? ClientId { get; set; } // Foreign Key for Client
        [ForeignKey("ClientId")]
        public virtual Client? Client { get; set; }

        [Display(Name = "Связанная заявка")]
        public int? OrderId { get; set; } // Foreign Key for Order
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [StringLength(250, ErrorMessage = "Местоположение не может превышать 250 символов.")]
        [Display(Name = "Местоположение/Ссылка")]
        public string? Location { get; set; }

        // Basic validation: EndTime must be after StartTime
        // This is a more complex validation for EF Core model layer.
        // Often handled in ViewModel or service layer.
        // For now, we'll rely on controller/ViewModel validation for this.
        // public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        // {
        //     if (EndTime <= StartTime)
        //     {
        //         yield return new ValidationResult(
        //             "Время окончания должно быть позже времени начала.",
        //             new[] { nameof(EndTime), nameof(StartTime) });
        //     }
        // }
    }
}
