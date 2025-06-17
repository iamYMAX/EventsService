using EventsService.Models; // For OrderStatus enum
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventsService.ViewModels
{
    public class EditOrderViewModel
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Клиент обязателен.")]
        [Display(Name = "Клиент")]
        public int ClientId { get; set; }
        public SelectList? Clients { get; set; }
        public string? ClientName { get; set; } // For display if ClientId is not editable


        [Display(Name = "Ответственный сотрудник")]
        public int? SalesRepresentativeId { get; set; }
        public SelectList? SalesRepresentatives { get; set; }
        public string? SalesRepresentativeName { get; set; } // For display if SalesRepId is not editable


        [Required(ErrorMessage = "Дата заявки обязательна.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Дата заявки")]
        public DateTime OrderDate { get; set; }

        [Required(ErrorMessage = "Статус заявки обязателен.")]
        [Display(Name = "Статус заявки")]
        public OrderStatus Status { get; set; }
        public SelectList? Statuses { get; set; }


        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();

        // Properties for available products to add to the order
        public MultiSelectList? AvailableProducts { get; set; }

        [Display(Name = "Добавить продукты/услуги")]
        public List<int>? SelectedProductIds { get; set; }


        // Role flags for conditional UI rendering (to be set by controller)
        public bool IsAdmin { get; set; }
        public bool IsManager { get; set; }
        public bool IsSalesRepresentative { get; set; }

        // Permission flags for specific fields (to be set by controller based on role and logic)
        public bool CanEditClient { get; set; }
        public bool CanEditSalesRepresentative { get; set; }
        public bool CanEditOrderDate { get; set; }
        public bool CanEditStatus { get; set; }
        public bool CanEditOrderItems { get; set; } // General permission to modify items (e.g. Qty, Price)
        public bool CanAddOrderItems { get; set; }
        public bool CanDeleteOrderItems { get; set; }


        public EditOrderViewModel()
        {
            OrderItems = new List<OrderItemViewModel>();
            SelectedProductIds = new List<int>();
        }
    }
}
