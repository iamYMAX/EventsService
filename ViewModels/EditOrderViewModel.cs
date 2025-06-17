using EventsService.Models; // For OrderStatus
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System; // Required for DateTime, added this as it's used by OrderDate

namespace EventsService.ViewModels
{
    public class EditOrderViewModel
    {
        public int Id { get; set; } // Order ID

        [Required]
        [Display(Name = "Client")]
        public int ClientId { get; set; }
        public SelectList? Clients { get; set; }

        [Display(Name = "Sales Representative")]
        public string? SalesRepresentativeId { get; set; } // Nullable if it can be unassigned by Admin/Manager. String because User.Id is typically string.
        public SelectList? SalesRepresentatives { get; set; }

        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; }

        [Required]
        public OrderStatus Status { get; set; }
        // Consider creating a SelectList for OrderStatus as well, if it's user-editable
        public SelectList? Statuses { get; set; }


        [Required]
        [Display(Name = "Order Items")]
        [MinLength(1, ErrorMessage = "Please add at least one product to the order.")]
        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();

        // For populating the product selection in dynamic rows
        public SelectList? ProductList { get; set; }
        // Corrected to use ProductInfoForJs directly as it's in the same namespace
        public List<ProductInfoForJs> ProductDetailsForJs { get; set; } = new List<ProductInfoForJs>();


        // Constructor to help initialize collections
        public EditOrderViewModel()
        {
            OrderItems = new List<OrderItemViewModel>();
            ProductDetailsForJs = new List<ProductInfoForJs>();
        }
    }
}
