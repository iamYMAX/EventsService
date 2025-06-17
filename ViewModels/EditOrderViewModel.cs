// Path: ViewModels/EditOrderViewModel.cs
using EventsService.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
// Assuming ProductInfoForJs is accessible via EventsService.ViewModels.ProductInfoForJs
// or by adding: using static EventsService.ViewModels.CreateOrderViewModel; if it were nested and public static
// For simplicity, assuming ProductInfoForJs is available via its namespace.

namespace EventsService.ViewModels
{
    public class EditOrderViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Client")]
        public int ClientId { get; set; }
        public SelectList? Clients { get; set; }

        [Display(Name = "Sales Representative")]
        public string? SalesRepresentativeId { get; set; } // Corrected type
        public SelectList? SalesRepresentatives { get; set; }

        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; }

        [Required]
        public OrderStatus Status { get; set; }
        public SelectList? Statuses { get; set; }

        [Required]
        [Display(Name = "Order Items")]
        [MinLength(1, ErrorMessage = "Please add at least one product to the order.")]
        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();

        public SelectList? ProductList { get; set; }
        public List<ProductInfoForJs> ProductDetailsForJs { get; set; } = new List<ProductInfoForJs>();

        public EditOrderViewModel()
        {
            OrderItems = new List<OrderItemViewModel>();
            ProductDetailsForJs = new List<ProductInfoForJs>();
        }
    }
}
