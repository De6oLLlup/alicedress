using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace alicedress.Models
{
    public class Order
    {
        public int Id { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        [Range(0.01, 10000000)]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Новый";

        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string DeliveryMethod { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Comment { get; set; } = string.Empty;

        // Навигационные свойства
        public virtual ApplicationUser? User { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}