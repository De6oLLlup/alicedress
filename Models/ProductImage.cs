using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace alicedress.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 1;

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}