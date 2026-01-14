using System.ComponentModel.DataAnnotations;

namespace alicedress.Models
{
    public class OrderViewModel
    {
        [Required(ErrorMessage = "ФИО обязательно")]
        [Display(Name = "ФИО")]
        [StringLength(100, ErrorMessage = "ФИО не должно превышать 100 символов")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный email адрес")]
        [Display(Name = "Email")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Телефон обязателен")]
        [Phone(ErrorMessage = "Некорректный номер телефона")]
        [Display(Name = "Телефон")]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Адрес доставки обязателен")]
        [Display(Name = "Адрес доставки")]
        [StringLength(200, ErrorMessage = "Адрес не должен превышать 200 символов")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Выберите способ доставки")]
        [Display(Name = "Способ доставки")]
        public string DeliveryMethod { get; set; } = string.Empty;

        [Required(ErrorMessage = "Выберите способ оплаты")]
        [Display(Name = "Способ оплаты")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Display(Name = "Комментарий к заказу")]
        [StringLength(500, ErrorMessage = "Комментарий не должен превышать 500 символов")]
        public string Comment { get; set; } = string.Empty;
    }
}