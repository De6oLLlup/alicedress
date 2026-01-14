using alicedress.Data;
using alicedress.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace alicedress.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<CheckoutController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                var cartItems = GetCartItems();
                if (!cartItems.Any())
                {
                    TempData["Error"] = "Ваша корзина пуста";
                    return RedirectToAction("Index", "Cart");
                }

                var model = new OrderViewModel();

                if (User.Identity.IsAuthenticated)
                {
                    var user = _userManager.GetUserAsync(User).Result;
                    if (user != null)
                    {
                        model.CustomerName = $"{user.FirstName} {user.LastName}";
                        model.CustomerEmail = user.Email;
                        model.CustomerPhone = user.PhoneNumber;
                        model.ShippingAddress = user.Address ?? "";
                    }
                }

                ViewBag.CartItems = cartItems;
                ViewBag.SubTotal = cartItems.Sum(item => item.TotalPrice);
                ViewBag.DeliveryCost = CalculateDeliveryCost("Самовывоз");
                ViewBag.Total = ViewBag.SubTotal + ViewBag.DeliveryCost;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке страницы оформления заказа");
                TempData["Error"] = "Произошла ошибка. Пожалуйста, попробуйте снова.";
                return RedirectToAction("Index", "Cart");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(OrderViewModel model)
        {
            _logger.LogInformation("Начало оформления заказа пользователем: {User}", User.Identity?.Name);

            List<CartItem> cartItems = new List<CartItem>();

            try
            {
                // Проверка валидации
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Модель заказа невалидна. Ошибки: {@Errors}",
                        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

                    cartItems = GetCartItems();
                    SetupViewBagForCheckout(cartItems, model.DeliveryMethod ?? "Самовывоз");
                    return View("Index", model);
                }

                _logger.LogInformation("Модель заказа валидна");

                // Получаем корзину
                cartItems = GetCartItems();
                _logger.LogInformation("Товаров в корзине: {Count}", cartItems.Count);

                if (!cartItems.Any())
                {
                    TempData["Error"] = "Ваша корзина пуста";
                    return RedirectToAction("Index", "Cart");
                }

                // Проверка наличия товаров и их количества
                var errors = new List<string>();
                foreach (var cartItem in cartItems)
                {
                    var product = await _context.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == cartItem.ProductId);

                    if (product == null)
                    {
                        errors.Add($"Товар '{cartItem.ProductName}' не найден в базе данных");
                        continue;
                    }

                    if (!product.IsActive)
                    {
                        errors.Add($"Товар '{cartItem.ProductName}' временно недоступен");
                        continue;
                    }

                    if (product.StockQuantity < cartItem.Quantity)
                    {
                        errors.Add($"Товара '{cartItem.ProductName}' недостаточно на складе. Доступно: {product.StockQuantity}");
                        continue;
                    }
                }

                if (errors.Any())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError("", error);
                    }

                    cartItems = GetCartItems();
                    SetupViewBagForCheckout(cartItems, model.DeliveryMethod ?? "Самовывоз");
                    return View("Index", model);
                }

                // Расчет суммы
                decimal deliveryCost = CalculateDeliveryCost(model.DeliveryMethod);
                decimal subtotal = cartItems.Sum(item => item.TotalPrice);
                decimal totalAmount = Math.Round(subtotal + deliveryCost, 2);

                _logger.LogInformation("Расчет суммы: Subtotal={Subtotal}, Delivery={Delivery}, Total={Total}",
                    subtotal, deliveryCost, totalAmount);

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Создаем заказ
                    var order = new Order
                    {
                        CustomerName = model.CustomerName?.Trim(),
                        CustomerEmail = model.CustomerEmail?.Trim(),
                        CustomerPhone = model.CustomerPhone?.Trim(),
                        ShippingAddress = model.ShippingAddress?.Trim(),
                        DeliveryMethod = model.DeliveryMethod,
                        PaymentMethod = model.PaymentMethod,
                        Comment = model.Comment?.Trim(),
                        OrderDate = DateTime.Now,
                        Status = "Новый",
                        TotalAmount = totalAmount
                    };

                    if (User.Identity.IsAuthenticated)
                    {
                        var user = await _userManager.GetUserAsync(User);
                        order.UserId = user?.Id;
                        _logger.LogInformation("Заказ для пользователя: {UserId}", user?.Id);
                    }
                    else
                    {
                        _logger.LogInformation("Заказ для гостя");
                    }

                    _context.Orders.Add(order);

                    // Сохраняем заказ, чтобы получить Id
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Заказ создан с ID: {OrderId}", order.Id);

                    // Добавляем товары в заказ
                    foreach (var cartItem in cartItems)
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = order.Id,
                            ProductId = cartItem.ProductId,
                            ProductName = cartItem.ProductName,
                            Quantity = cartItem.Quantity,
                            UnitPrice = cartItem.Price,
                            TotalPrice = cartItem.TotalPrice
                        };
                        _context.OrderItems.Add(orderItem);

                        // Обновляем количество товара на складе
                        var product = await _context.Products.FindAsync(cartItem.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity -= cartItem.Quantity;
                            if (product.StockQuantity < 0)
                                product.StockQuantity = 0;

                            _context.Products.Update(product);
                            _logger.LogInformation("Обновлен запас товара {ProductId}: -{Quantity}",
                                product.Id, cartItem.Quantity);
                        }
                    }

                    // Сохраняем все изменения
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Заказ {OrderId} успешно сохранен. Товаров: {ItemsCount}",
                        order.Id, cartItems.Count);

                    // Очищаем корзину
                    HttpContext.Session.Remove("Cart");

                    TempData["OrderId"] = order.Id;
                    TempData["Success"] = $"Заказ №{order.Id} успешно оформлен!";

                    return RedirectToAction("Confirmation", new { id = order.Id });
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();

                    // Логируем внутренние исключения
                    var innerException = dbEx.InnerException;
                    while (innerException != null)
                    {
                        _logger.LogError(innerException, "Внутреннее исключение БД: {Message}",
                            innerException.Message);
                        innerException = innerException.InnerException;
                    }

                    _logger.LogError(dbEx, "Ошибка базы данных при сохранении заказа");

                    // Более конкретные сообщения об ошибках
                    if (dbEx.InnerException?.Message.Contains("FOREIGN KEY") == true)
                    {
                        ModelState.AddModelError("", "Ошибка связей между данными. Возможно, товар был удален.");
                    }
                    else if (dbEx.InnerException?.Message.Contains("PRIMARY KEY") == true)
                    {
                        ModelState.AddModelError("", "Ошибка уникальности данных.");
                    }
                    else if (dbEx.InnerException?.Message.Contains("cannot insert null") == true)
                    {
                        ModelState.AddModelError("", "Не все обязательные поля заполнены.");
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Ошибка базы данных: {dbEx.InnerException?.Message ?? dbEx.Message}");
                    }

                    cartItems = GetCartItems();
                    SetupViewBagForCheckout(cartItems, model.DeliveryMethod ?? "Самовывоз");
                    return View("Index", model);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Непредвиденная ошибка в транзакции");
                    ModelState.AddModelError("", $"Непредвиденная ошибка: {ex.Message}");

                    cartItems = GetCartItems();
                    SetupViewBagForCheckout(cartItems, model.DeliveryMethod ?? "Самовывоз");
                    return View("Index", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при оформлении заказа");
                TempData["Error"] = $"Произошла критическая ошибка: {ex.Message}";

                return RedirectToAction("Index", "Cart");
            }
        }

        [HttpGet]
        public IActionResult Confirmation(int id)
        {
            try
            {
                var order = _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefault(o => o.Id == id);

                if (order == null)
                {
                    TempData["Error"] = "Заказ не найден";
                    return RedirectToAction("Index", "Home");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке страницы подтверждения заказа");
                TempData["Error"] = "Произошла ошибка";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public IActionResult CalculateTotal(string deliveryMethod)
        {
            try
            {
                var currentCartItems = GetCartItems();
                decimal subtotal = currentCartItems.Sum(item => item.TotalPrice);
                decimal deliveryCost = CalculateDeliveryCost(deliveryMethod);
                decimal total = subtotal + deliveryCost;

                return Json(new
                {
                    Success = true,
                    SubTotal = subtotal,
                    DeliveryCost = deliveryCost,
                    Total = total,
                    DeliveryMethod = deliveryMethod
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при расчете общей суммы");
                return Json(new { Success = false, Message = "Ошибка расчета" });
            }
        }

        private decimal CalculateDeliveryCost(string deliveryMethod)
        {
            if (string.IsNullOrEmpty(deliveryMethod))
                return 0;

            return deliveryMethod switch
            {
                "Курьер" => 300,
                "Почта России" => 200,
                "СДЭК" => 250,
                _ => 0
            };
        }

        private List<CartItem> GetCartItems()
        {
            try
            {
                var cartJson = HttpContext.Session.GetString("Cart");
                if (string.IsNullOrEmpty(cartJson))
                    return new List<CartItem>();

                return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении корзины из сессии");
                return new List<CartItem>();
            }
        }

        private void SetupViewBagForCheckout(List<CartItem> cartItems, string deliveryMethod)
        {
            ViewBag.CartItems = cartItems;
            ViewBag.SubTotal = cartItems.Sum(item => item.TotalPrice);
            ViewBag.DeliveryCost = CalculateDeliveryCost(deliveryMethod);
            ViewBag.Total = ViewBag.SubTotal + ViewBag.DeliveryCost;
        }
    }
}