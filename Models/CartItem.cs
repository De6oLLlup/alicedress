using System.ComponentModel.DataAnnotations;

namespace alicedress.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CartId { get; set; } = string.Empty; // Session ID или User ID

        [Required]
        public int ProductId { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

/*        public string? Size { get; set; }

        public string? Color { get; set; }*/

        public DateTime AddedDate { get; set; } = DateTime.Now;

        // Вычисляемое свойство
        public decimal TotalPrice => Price * Quantity;
    }
}