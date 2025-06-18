using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace EventsService.ViewModels
{
    public class CartViewModel
    {
        public int Id { get; set; } // Cart.Id
        public int ClientId { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();

        [Display(Name = "Итоговая сумма по корзине")]
        [DataType(DataType.Currency)]
        public decimal GrandTotal => Items.Sum(item => item.LineItemTotal); // Calculated property
    }
}
