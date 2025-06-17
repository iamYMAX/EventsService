using EventsService.Models; // For OrderStatus enum
using System;
using System.ComponentModel.DataAnnotations;

namespace EventsService.ViewModels
{
    public class OrderViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Клиент")]
        public string ClientName { get; set; } = string.Empty;

        [Display(Name = "Ответственный сотрудник")]
        public string SalesRepresentativeDisplay { get; set; } = "Не назначен"; // e.g., "John Doe (Sales Rep)" or "Not assigned"

        [Display(Name = "Дата заявки")]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH:mm}")]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Статус")]
        public OrderStatus Status { get; set; }

        // Optional: Add other properties you might want to display from the Order model
        // public decimal TotalAmount { get; set; } // Example if you calculate it
        // public int NumberOfItems { get; set; } // Example
    }
}
