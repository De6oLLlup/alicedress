using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using alicedress.Data;
using alicedress.Models;
using System.Threading.Tasks;

namespace alicedress.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                TotalProducts = await _context.Products.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(),
                TotalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount),
                RecentOrders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToListAsync(),
                LowStockProducts = await _context.Products
                    .Where(p => p.StockQuantity < 10)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }

        public class DashboardViewModel
        {
            public int TotalProducts { get; set; }
            public int TotalOrders { get; set; }
            public int TotalUsers { get; set; }
            public decimal TotalRevenue { get; set; }
            public List<Order> RecentOrders { get; set; } = new();
            public List<Product> LowStockProducts { get; set; } = new();
        }
    }
}