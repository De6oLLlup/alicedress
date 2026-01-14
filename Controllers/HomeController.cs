using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using alicedress.Data;
using System.Threading.Tasks;
using System.Linq;

namespace alicedress.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var bestsellers = await _context.Products
                    .Where(p => p.IsBestseller && p.IsActive)
                    .Include(p => p.Images)
                    .Take(8)
                    .ToListAsync();

                // Получаем категории для секции "Популярные категории"
                var categories = await _context.Products
                    .Where(p => p.IsActive)
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .Take(6) // Берем только 6 категорий
                    .ToListAsync();

                ViewBag.Categories = categories;

                return View(bestsellers);
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Ошибка в HomeController.Index: {ex.Message}");

                // Возвращаем пустой список, чтобы не падать
                ViewBag.Categories = new List<string>();
                return View(new List<Models.Product>());
            }
        }
    }
}