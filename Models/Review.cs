using System.ComponentModel.DataAnnotations;

namespace alicedress.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Навигационные свойства
        public virtual Product? Product { get; set; }
        public virtual ApplicationUser? ApplicationUser { get; set; }
    }
}