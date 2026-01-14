// Controllers/ProductController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using alicedress.Data;
using System.Threading.Tasks;

namespace alicedress.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Product/Details/5
        [Route("Product/Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Загружаем отзывы для этого товара
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == id)
                .ToListAsync();

            ViewBag.Reviews = reviews;

            // Получаем похожие товары (той же категории)
            var relatedProducts = await _context.Products
                .Where(p => p.Category == product.Category && p.Id != id && p.IsActive)
                .Include(p => p.Images)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }
    }
}