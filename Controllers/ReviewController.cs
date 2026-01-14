using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using alicedress.Data;
using alicedress.Models;
using System.Linq;
using System.Threading.Tasks;

namespace alicedress.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(int productId, int rating, string comment)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _context.Products.FindAsync(productId);

            if (product == null)
            {
                return NotFound();
            }

            var review = new Review
            {
                ProductId = productId,
                UserId = user.Id,
                Rating = rating,
                Comment = comment,
                CreatedDate = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Product", new { id = productId });
        }
    }
}