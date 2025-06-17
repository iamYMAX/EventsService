using EventsService.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventsService.ViewModels
{
    public class CreateOrderViewModel
    {
        [Required(ErrorMessage = "Необходимо выбрать клиента.")]
        [Display(Name = "Клиент")]
        public int ClientId { get; set; }
        public SelectList? Clients { get; set; } // For dropdown list of clients

        [Display(Name = "Ответственный сотрудник (Торговый представитель)")]
        public int? SalesRepresentativeId { get; set; } // Nullable, can be assigned later
        public SelectList? SalesRepresentatives { get; set; } // For dropdown list of users in SalesRepresentative role

        [Required(ErrorMessage = "Дата заявки обязательна.")]
        [Display(Name = "Дата заявки")]
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Статус заявки")]
        public OrderStatus Status { get; set; } = OrderStatus.New;

        // For selecting products to add to the order
        // This part might be more complex in a real UI (e.g., dynamic rows, search)
        // For now, let's assume we can select multiple products.
        // A more robust solution would involve JavaScript to dynamically add items.
        [Required(ErrorMessage = "Необходимо выбрать хотя бы один товар или услугу.")]
        [Display(Name = "Товары/Услуги")]
        public List<int> SelectedProductIds { get; set; } = new List<int>();
        public MultiSelectList? Products { get; set; } // For multi-select list of products

        // You might also want to include quantities for each product here if adding them directly on this form
        // For simplicity now, we'll add products without specifying quantity on this initial form.
        // Quantity can be managed on an "Edit Order" screen or default to 1.
    }
}
