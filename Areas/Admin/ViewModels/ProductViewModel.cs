using System.ComponentModel.DataAnnotations;
using alicedress.Models;

namespace alicedress.Areas.Admin.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название обязательно")]
        [Display(Name = "Название")]
        [StringLength(200, ErrorMessage = "Название не должно превышать 200 символов")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Описание")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Цена обязательна")]
        [Display(Name = "Цена")]
        [Range(0, 1000000, ErrorMessage = "Цена должна быть от 0 до 1,000,000")]
        public decimal Price { get; set; }

        [Display(Name = "Категория")]
        [StringLength(100, ErrorMessage = "Категория не должна превышать 100 символов")]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "Состав")]
        public string Composition { get; set; } = string.Empty;

        [Display(Name = "Артикул")]
        [StringLength(50, ErrorMessage = "Артикул не должен превышать 50 символов")]
        public string Article { get; set; } = string.Empty;

        [Display(Name = "Активен")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Бестселлер")]
        public bool IsBestseller { get; set; }

        [Display(Name = "Количество на складе")]
        [Range(0, 10000, ErrorMessage = "Количество должно быть от 0 до 10,000")]
        public int StockQuantity { get; set; } = 100;

        [Display(Name = "Изображения")]
        public List<IFormFile>? Images { get; set; }

        [Display(Name = "Удалить изображения")]
        public List<int>? ImagesToDelete { get; set; }

        public List<ProductImage> ExistingImages { get; set; } = new();
    }
}