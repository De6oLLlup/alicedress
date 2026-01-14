using System.ComponentModel.DataAnnotations;

namespace alicedress.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

/*        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;*/

        public virtual Order? Order { get; set; }
        public virtual Product? Product { get; set; }
    }
}