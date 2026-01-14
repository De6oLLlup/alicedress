using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using alicedress.Data;
using alicedress.Models;
using alicedress.Areas.Admin.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace alicedress.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Admin/Products
        public async Task<IActionResult> Index(string search = null, string category = null, int page = 1)
        {
            const int pageSize = 10;

            // Получаем все категории для фильтра
            var categories = await _context.Products
                .Where(p => p.IsActive)
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;
            ViewBag.Search = search;

            var query = _context.Products
                .Include(p => p.Images)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Article.Contains(search) ||
                    p.Description.Contains(search));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category == category);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var products = await query
                .OrderByDescending(p => p.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(products);
        }

        // GET: /Admin/Products/Create
        public IActionResult Create()
        {
            // Получаем существующие категории для подсказки
            var categories = _context.Products
                .Where(p => p.IsActive)
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            ViewBag.Categories = categories;
            return View();

        }

        // POST: /Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var product = new Product
                    {
                        Name = model.Name,
                        Description = model.Description,
                        Price = model.Price,
                        Category = model.Category,
                        Composition = model.Composition,
                        Article = model.Article,
                        IsActive = model.IsActive,
                        IsBestseller = model.IsBestseller,
                        StockQuantity = model.StockQuantity,
                        CreatedDate = DateTime.Now
                    };

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    // Обработка изображений
                    if (model.Images != null && model.Images.Any())
                    {
                        var uploadsPath = Path.Combine(_environment.WebRootPath, "images", "products");
                        if (!Directory.Exists(uploadsPath))
                        {
                            Directory.CreateDirectory(uploadsPath);
                        }

                        for (int i = 0; i < model.Images.Count; i++)
                        {
                            var image = model.Images[i];
                            if (image.Length > 0)
                            {
                                var fileName = $"{product.Id}_{i}_{DateTime.Now.Ticks}{Path.GetExtension(image.FileName)}";
                                var filePath = Path.Combine(uploadsPath, fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await image.CopyToAsync(stream);
                                }

                                _context.ProductImages.Add(new ProductImage
                                {
                                    ProductId = product.Id,
                                    ImageUrl = $"/images/products/{fileName}",
                                    DisplayOrder = i + 1
                                });
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["Success"] = "Товар успешно добавлен!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Ошибка при сохранении: {ex.Message}");

                    // Повторно загружаем категории в случае ошибки
                    var categories = _context.Products
                        .Where(p => p.IsActive)
                        .Select(p => p.Category)
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList();

                    ViewBag.Categories = categories;
                }
            }
            else
            {
                // Загружаем категории при невалидной модели
                var categories = _context.Products
                    .Where(p => p.IsActive)
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                ViewBag.Categories = categories;
            }

            return View(model);
        }

        // GET: /Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Загружаем категории для выпадающего списка
            var categories = await _context.Products
                .Where(p => p.IsActive)
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Categories = categories;

            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Category = product.Category,
                Composition = product.Composition,
                Article = product.Article,
                IsActive = product.IsActive,
                IsBestseller = product.IsBestseller,
                StockQuantity = product.StockQuantity,
                ExistingImages = product.Images.ToList()
            };

            return View(model);
        }

        // POST: /Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var product = await _context.Products
                        .Include(p => p.Images)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (product == null)
                    {
                        return NotFound();
                    }

                    product.Name = model.Name;
                    product.Description = model.Description;
                    product.Price = model.Price;
                    product.Category = model.Category;
                    product.Composition = model.Composition;
                    product.Article = model.Article;
                    product.IsActive = model.IsActive;
                    product.IsBestseller = model.IsBestseller;
                    product.StockQuantity = model.StockQuantity;

                    // Удаление изображений
                    if (model.ImagesToDelete != null && model.ImagesToDelete.Any())
                    {
                        foreach (var imageId in model.ImagesToDelete)
                        {
                            var image = await _context.ProductImages.FindAsync(imageId);
                            if (image != null)
                            {
                                // Удаление физического файла
                                var filePath = Path.Combine(_environment.WebRootPath,
                                    image.ImageUrl.TrimStart('/'));
                                if (System.IO.File.Exists(filePath))
                                {
                                    System.IO.File.Delete(filePath);
                                }

                                _context.ProductImages.Remove(image);
                            }
                        }
                    }

                    // Добавление новых изображений
                    if (model.Images != null && model.Images.Any())
                    {
                        var uploadsPath = Path.Combine(_environment.WebRootPath, "images", "products");
                        if (!Directory.Exists(uploadsPath))
                        {
                            Directory.CreateDirectory(uploadsPath);
                        }

                        var maxOrder = product.Images.Any() ? product.Images.Max(i => i.DisplayOrder) : 0;
                        var startOrder = maxOrder + 1;

                        for (int i = 0; i < model.Images.Count; i++)
                        {
                            var image = model.Images[i];
                            if (image.Length > 0)
                            {
                                var fileName = $"{product.Id}_{startOrder + i}_{DateTime.Now.Ticks}{Path.GetExtension(image.FileName)}";
                                var filePath = Path.Combine(uploadsPath, fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await image.CopyToAsync(stream);
                                }

                                _context.ProductImages.Add(new ProductImage
                                {
                                    ProductId = product.Id,
                                    ImageUrl = $"/images/products/{fileName}",
                                    DisplayOrder = startOrder + i
                                });
                            }
                        }
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Товар успешно обновлен!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Ошибка при обновлении: {ex.Message}");
                }
            }

            // Загружаем категории при ошибке
            var categories = await _context.Products
                .Where(p => p.IsActive)
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Categories = categories;

            return View(model);
        }

        // GET: /Admin/Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: /Admin/Products/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: /Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product != null)
            {
                // Удаление изображений
                foreach (var image in product.Images)
                {
                    var filePath = Path.Combine(_environment.WebRootPath,
                        image.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Товар успешно удален!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}