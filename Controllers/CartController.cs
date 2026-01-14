using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using alicedress.Data;
using alicedress.Models;
using System.Text.Json;
using System.Linq;

namespace alicedress.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            try
            {
                var cartItems = GetCartItems();
                var model = new CartViewModel
                {
                    CartItems = cartItems,
                    SubTotal = cartItems.Sum(item => item.TotalPrice),
                    DeliveryCost = 0, // По умолчанию самовывоз
                    Total = cartItems.Sum(item => item.TotalPrice)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке корзины");
                return View(new CartViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, string? size = null, string? color = null)
        {
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                {
                    TempData["Error"] = "Товар не найден";
                    return RedirectToAction("Index", "Catalog");
                }

                if (quantity <= 0)
                    quantity = 1;

                if (product.StockQuantity < quantity)
                {
                    TempData["Error"] = $"Недостаточно товара на складе. Доступно: {product.StockQuantity}";
                    return RedirectToAction("Details", "Product", new { id = productId });
                }

                var cart = GetCartItems();

                var existingItem = cart.FirstOrDefault(x => x.ProductId == productId);

                if (existingItem != null)
                {
                    if (existingItem.Quantity + quantity > product.StockQuantity)
                    {
                        TempData["Error"] = $"Недостаточно товара на складе. Доступно: {product.StockQuantity}";
                        return RedirectToAction("Details", "Product", new { id = productId });
                    }

                    existingItem.Quantity += quantity;
                    existingItem.AddedDate = DateTime.Now;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        Id = cart.Count > 0 ? cart.Max(c => c.Id) + 1 : 1,
                        CartId = GetCartId(),
                        ProductId = productId,
                        ProductName = product.Name,
                        Price = product.Price,
                        Quantity = quantity,
                        AddedDate = DateTime.Now
                    });
                }

                SaveCartItems(cart);
                TempData["Success"] = $"{product.Name} добавлен в корзину";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении товара в корзину");
                TempData["Error"] = "Ошибка при добавлении товара в корзину";
                return RedirectToAction("Index", "Catalog");
            }
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int itemId, int quantity)
        {
            try
            {
                var cart = GetCartItems();
                var item = cart.FirstOrDefault(x => x.Id == itemId);

                if (item != null)
                {
                    if (quantity > 0)
                    {
                        // Получаем информацию о товаре для проверки остатков
                        var product = _context.Products.Find(item.ProductId);
                        if (product != null && quantity > product.StockQuantity)
                        {
                            TempData["Error"] = $"Недостаточно товара на складе. Доступно: {product.StockQuantity}";
                            return RedirectToAction("Index");
                        }

                        item.Quantity = quantity;
                    }
                    else
                    {
                        cart.Remove(item);
                    }

                    SaveCartItems(cart);
                    TempData["Success"] = "Корзина обновлена";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении количества товара");
                TempData["Error"] = "Ошибка при обновлении корзины";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult RemoveItem(int itemId)
        {
            try
            {
                var cart = GetCartItems();
                var item = cart.FirstOrDefault(x => x.Id == itemId);

                if (item != null)
                {
                    cart.Remove(item);
                    SaveCartItems(cart);
                    TempData["Success"] = "Товар удален из корзины";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении товара из корзины");
                TempData["Error"] = "Ошибка при удалении товара";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult Clear()
        {
            try
            {
                HttpContext.Session.Remove("Cart");
                TempData["Success"] = "Корзина очищена";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке корзины");
                TempData["Error"] = "Ошибка при очистке корзины";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult CalculateTotal(string deliveryMethod)
        {
            try
            {
                var cartItems = GetCartItems();
                decimal subtotal = cartItems.Sum(item => item.TotalPrice);
                decimal deliveryCost = CalculateDeliveryCost(deliveryMethod);
                decimal total = subtotal + deliveryCost;

                return Json(new
                {
                    Success = true,
                    SubTotal = subtotal,
                    DeliveryCost = deliveryCost,
                    Total = total
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при расчете суммы заказа");
                return Json(new { Success = false, Message = "Ошибка расчета" });
            }
        }

        private decimal CalculateDeliveryCost(string deliveryMethod)
        {
            return deliveryMethod switch
            {
                "Курьер" => 300,
                "Почта России" => 200,
                "СДЭК" => 250,
                _ => 0 // Самовывоз
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
                _logger.LogError(ex, "Ошибка при десериализации корзины");
                return new List<CartItem>();
            }
        }

        private void SaveCartItems(List<CartItem> items)
        {
            try
            {
                var cartJson = JsonSerializer.Serialize(items);
                HttpContext.Session.SetString("Cart", cartJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении корзины");
            }
        }

        private string GetCartId()
        {
            return HttpContext.Session.Id;
        }
    }
}