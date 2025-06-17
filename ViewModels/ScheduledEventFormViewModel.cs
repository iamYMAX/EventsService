using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventsService.ViewModels
{
    public class ScheduledEventFormViewModel
    {
        public int Id { get; set; } // 0 for Create, >0 for Edit

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
        public DateTime StartTime { get; set; } = DateTime.Now; // Default to now

        [Required(ErrorMessage = "Время окончания обязательно.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Время окончания")]
        public DateTime EndTime { get; set; } = DateTime.Now.AddHours(1); // Default to one hour from now

        [Display(Name = "Ответственный сотрудник")]
        public int? UserId { get; set; }
        public SelectList? Users { get; set; } // For dropdown

        [Display(Name = "Клиент")]
        public int? ClientId { get; set; }
        public SelectList? Clients { get; set; } // For dropdown

        [Display(Name = "Связанная заявка")]
        public int? OrderId { get; set; }
        public SelectList? Orders { get; set; } // For dropdown

        [StringLength(250, ErrorMessage = "Местоположение не может превышать 250 символов.")]
        [Display(Name = "Местоположение/Ссылка")]
        public string? Location { get; set; }

        // Custom validation for StartTime < EndTime
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndTime <= StartTime)
            {
                yield return new ValidationResult(
                    "Время окончания должно быть позже времени начала.",
                    new[] { nameof(EndTime), nameof(StartTime) });
            }
        }
    }
}
