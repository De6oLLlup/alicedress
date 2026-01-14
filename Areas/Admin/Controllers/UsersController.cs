using alicedress.Data;
using alicedress.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace alicedress.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index(
            string search = null,
            string role = null,
            int page = 1,
            int pageSize = 20)
        {
            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                usersQuery = usersQuery.Where(u =>
                    u.UserName.Contains(search) ||
                    u.Email.Contains(search) ||
                    u.FirstName.Contains(search) ||
                    u.LastName.Contains(search));
            }

            var totalItems = await usersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var users = await usersQuery
                .OrderByDescending(u => u.RegistrationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Получаем роли для каждого пользователя
            var userRoles = new Dictionary<string, List<string>>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.ToList();
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(users);
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new UserDetailsViewModel
            {
                User = user,
                Roles = await _userManager.GetRolesAsync(user),
                Orders = await _context.Orders
                    .Where(o => o.UserId == id)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToListAsync(),
                Reviews = await _context.Reviews
                    .Where(r => r.UserId == id)
                    .Include(r => r.Product)
                    .Take(10)
                    .ToListAsync()
            };

            return View(model);
        }

        // GET: Admin/Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList()
            };

            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        // POST: Admin/Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                user.Email = model.Email;
                user.UserName = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;
                user.EmailConfirmed = model.EmailConfirmed;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    // Обновление ролей
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                    if (model.SelectedRoles != null)
                    {
                        await _userManager.AddToRolesAsync(user, model.SelectedRoles);
                    }

                    TempData["Success"] = "Пользователь успешно обновлен";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        // GET: Admin/Users/Create
        public IActionResult Create()
        {
            ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View(new CreateUserViewModel());
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    EmailConfirmed = true,
                    RegistrationDate = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (model.SelectedRoles != null && model.SelectedRoles.Any())
                    {
                        await _userManager.AddToRolesAsync(user, model.SelectedRoles);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "Customer");
                    }

                    TempData["Success"] = "Пользователь успешно создан";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        // POST: Admin/Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Проверяем, есть ли связанные заказы
            var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == id);
            if (hasOrders)
            {
                TempData["Error"] = "Невозможно удалить пользователя с существующими заказами";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Пользователь успешно удален";
            }
            else
            {
                TempData["Error"] = "Ошибка при удалении пользователя";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Users/ResetPassword
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var newPassword = GenerateRandomPassword();
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                TempData["Success"] = $"Пароль сброшен. Новый пароль: {newPassword}";
            }
            else
            {
                TempData["Error"] = "Ошибка при сбросе пароля";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/Users/ToggleLock
        [HttpPost]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (user.LockoutEnd > DateTime.Now)
            {
                // Разблокировать
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["Success"] = "Пользователь разблокирован";
            }
            else
            {
                // Заблокировать на 30 дней
                await _userManager.SetLockoutEndDateAsync(user, DateTime.Now.AddDays(30));
                TempData["Success"] = "Пользователь заблокирован на 30 дней";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        private string GenerateRandomPassword()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    public class UserDetailsViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public IList<string> Roles { get; set; } = new List<string>();
        public List<Order> Orders { get; set; } = new();
        public List<Review> Reviews { get; set; } = new();
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public bool EmailConfirmed { get; set; }

        public List<string> Roles { get; set; } = new();
        public List<string>? SelectedRoles { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }

        public List<string>? SelectedRoles { get; set; }
    }
}