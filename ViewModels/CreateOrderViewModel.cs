using EventsService.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System; // Required for DateTime

namespace EventsService.ViewModels
{
    public record ProductInfoForJs(string Value, string Text, decimal Price); // Moved to be clearly public at namespace level

    public class CreateOrderViewModel
    {
        [Required(ErrorMessage = "Необходимо выбрать клиента.")]
        [Display(Name = "Клиент")]
        public int ClientId { get; set; }
        public SelectList? Clients { get; set; }

        [Display(Name = "Ответственный сотрудник (Торговый представитель)")]
        public string? SalesRepresentativeId { get; set; } // Changed from int? to string?
        public SelectList? SalesRepresentatives { get; set; }

        [Required(ErrorMessage = "Дата заявки обязательна.")]
        [Display(Name = "Дата заявки")]
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Статус заявки")]
        public OrderStatus Status { get; set; } = OrderStatus.New;

        [Required]
        [Display(Name = "Order Items")]
        [MinLength(1, ErrorMessage = "Please add at least one product to the order.")]
        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();

        public SelectList? ProductList { get; set; }

        public List<ProductInfoForJs> ProductDetailsForJs { get; set; } = new List<ProductInfoForJs>();
    }
}
