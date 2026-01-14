using System.ComponentModel.DataAnnotations;

namespace alicedress.Models
{
    public class CartViewModel
    {
        public List<CartItem> CartItems { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal DeliveryCost { get; set; }
        public decimal Total { get; set; }
        public string SelectedDeliveryMethod { get; set; } = "Самовывоз";

        [Display(Name = "Адрес доставки")]
        public string ShippingAddress { get; set; } = string.Empty;
    }
}