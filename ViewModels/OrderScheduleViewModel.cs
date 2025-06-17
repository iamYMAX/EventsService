using System;
using System.ComponentModel.DataAnnotations; // For Display attribute if you add any later

namespace EventsService.ViewModels
{
    public class OrderScheduleViewModel // Renamed from OrderScheduleItemViewModel for final version
    {
        public int OrderId { get; set; }

        [Display(Name = "Планируемая дата/время")]
        public DateTime ScheduledServiceDateTime { get; set; }

        [Display(Name = "Клиент")]
        public string? ClientName { get; set; }

        [Display(Name = "Ответственный сотрудник")]
        public string? SalesRepresentativeName { get; set; }

        [Display(Name = "Статус заявки")]
        public string? OrderStatus { get; set; }

        [Display(Name = "Содержание заявки")]
        public string? OrderSummary { get; set; } // e.g., brief item list or title
    }
}
