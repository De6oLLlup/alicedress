using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using alicedress.Data;
using alicedress.Models;

namespace alicedress.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var stats = new
            {
                TotalProducts = _context.Products.Count(),
                TotalOrders = _context.Orders.Count(),
                TotalUsers = _context.Users.Count(),
                PendingOrders = _context.Orders.Count(o => o.Status == "Новый"),
                TotalRevenue = _context.Orders.Sum(o => o.TotalAmount)
            };

            ViewBag.Stats = stats;
            return View();
        }
    }
}