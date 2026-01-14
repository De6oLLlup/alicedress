using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using alicedress.Data;
using System.Linq;
using System.Threading.Tasks;

namespace alicedress.Controllers
{
    public class CatalogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CatalogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Catalog
        public async Task<IActionResult> Index(string category = null, string sort = "popular")
        {
            // Получаем активные товары из базы данных
            var query = _context.Products
                .Where(p => p.IsActive)
                .Include(p => p.Images) // Включаем изображения
                .AsQueryable();

            // Фильтрация по категории (если указана)
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category == category);
            }

            // Сортировка
            switch (sort)
            {
                case "price_asc":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                case "new":
                    query = query.OrderByDescending(p => p.CreatedDate);
                    break;
                case "popular":
                default:
                    query = query.OrderByDescending(p => p.IsBestseller)
                                 .ThenByDescending(p => p.CreatedDate);
                    break;
            }

            var products = await query.ToListAsync();

            // Получаем уникальные категории для фильтра
            var categories = await _context.Products
                .Where(p => p.IsActive)
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedSort = sort;

            return View(products);
        }
    }
}