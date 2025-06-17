using System;
using System.ComponentModel.DataAnnotations;

namespace EventsService.ViewModels
{
    public class ScheduledEventViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Заголовок")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Время начала")]
        public DateTime StartTime { get; set; }

        [Display(Name = "Время окончания")]
        public DateTime EndTime { get; set; }

        [Display(Name = "Ответственный сотрудник")]
        public string? UserName { get; set; } // DisplayName or UserName of the assigned User

        [Display(Name = "Клиент")]
        public string? ClientName { get; set; }

        [Display(Name = "Связанная заявка ID")]
        public int? OrderId { get; set; }
        // Could add more Order details if needed, e.g., Order.OrderNumber or a summary

        [Display(Name = "Местоположение/Ссылка")]
        public string? Location { get; set; }
    }
}
