using System.ComponentModel.DataAnnotations;

namespace EventsService.ViewModels
{
    public class CartItemViewModel
    {
        public int Id { get; set; } // CartItem.Id
        public int ProductId { get; set; }

        [Display(Name = "Продукт/Услуга")]
        public string ProductName { get; set; } = string.Empty;

        [Display(Name = "Количество")]
        public int Quantity { get; set; }

        [Display(Name = "Цена за единицу")]
        [DataType(DataType.Currency)]
        public decimal PricePerUnit { get; set; }

        [Display(Name = "Сумма по позиции")]
        [DataType(DataType.Currency)]
        public decimal LineItemTotal => Quantity * PricePerUnit; // Calculated property
    }
}
