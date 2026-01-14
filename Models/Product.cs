using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace alicedress.Models
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(200, ErrorMessage = "Название не должно превышать 200 символов")]
        [Display(Name = "Название")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Описание")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Цена обязательна")]
        [Range(0, 1000000, ErrorMessage = "Цена должна быть от 0 до 1,000,000")]
        [DataType(DataType.Currency)]
        [Display(Name = "Цена")]
        public decimal Price { get; set; }

        [StringLength(100, ErrorMessage = "Категория не должна превышать 100 символов")]
        [Display(Name = "Категория")]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "Состав")]
        public string Composition { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Артикул не должен превышать 50 символов")]
        [Display(Name = "Артикул")]
        public string Article { get; set; } = string.Empty;

        [Display(Name = "Активен")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Бестселлер")]
        public bool IsBestseller { get; set; }

        [Range(0, 10000, ErrorMessage = "Количество должно быть от 0 до 10,000")]
        [Display(Name = "Количество на складе")]
        public int StockQuantity { get; set; } = 100;

        [Display(Name = "Дата создания")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}