using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using alicedress.Data;
using alicedress.Models;
using System.Linq;
using System.Threading.Tasks;

namespace alicedress.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Orders
        public async Task<IActionResult> Index(
            string status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string search = null,
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "Все")
            {
                query = query.Where(o => o.Status == status);
            }

            if (startDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= endDate.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o =>
                    o.CustomerName.Contains(search) ||
                    o.CustomerEmail.Contains(search) ||
                    o.CustomerPhone.Contains(search) ||
                    o.Id.ToString().Contains(search));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Statuses = new List<string>
            {
                "Все", "Новый", "Подтвержден", "В обработке", "Отправлен", "Доставлен", "Отменен"
            };
            ViewBag.SelectedStatus = status;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRevenue = await query.SumAsync(o => o.TotalAmount);

            return View(orders);
        }

        // GET: Admin/Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Admin/Orders/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Admin/Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Order model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return NotFound();
                }

                order.Status = model.Status;
                order.CustomerName = model.CustomerName;
                order.CustomerEmail = model.CustomerEmail;
                order.CustomerPhone = model.CustomerPhone;
                order.ShippingAddress = model.ShippingAddress;
                order.DeliveryMethod = model.DeliveryMethod;
                order.PaymentMethod = model.PaymentMethod;
                order.Comment = model.Comment;

                _context.Update(order);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Заказ успешно обновлен";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // POST: Admin/Orders/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Статус заказа обновлен";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/Orders/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null)
            {
                // Восстановление остатков товаров
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                    }
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Заказ удален";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Orders/Export
        public async Task<IActionResult> Export(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string format = "excel")
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(o => o.OrderDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(o => o.OrderDate <= endDate.Value);

            var orders = await query.ToListAsync();

            if (format == "csv")
            {
                var csv = "ID;Дата;Клиент;Email;Телефон;Сумма;Статус\n";
                foreach (var order in orders)
                {
                    csv += $"{order.Id};{order.OrderDate:dd.MM.yyyy};{order.CustomerName};" +
                           $"{order.CustomerEmail};{order.CustomerPhone};" +
                           $"{order.TotalAmount};{order.Status}\n";
                }

                return File(System.Text.Encoding.UTF8.GetBytes(csv),
                    "text/csv", $"orders_{DateTime.Now:yyyyMMdd}.csv");
            }

            // Для Excel возвращаем JSON (в реальности нужно использовать библиотеку EPPlus или ClosedXML)
            return Json(orders);
        }
    }
}