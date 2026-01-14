using Microsoft.AspNetCore.Identity;
using alicedress.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace alicedress.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.Users.Any(u => u.Email == "admin@alicedress.ru" ||
                              u.Email == "customer@example.com"))
            {
                return; // Пользователи уже созданы
            }

            // ✅ Проверяем существование администратора
            var adminEmail = "admin@alicedress.ru";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            // ✅ Если администратора нет - создаем (дополнительная проверка)
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Администратор",
                    LastName = "Системы",
                    EmailConfirmed = true
                };

                var adminCreateResult = await userManager.CreateAsync(adminUser, "Admin123!");
                if (adminCreateResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // ✅ Проверяем существование тестового покупателя
            var customerEmail = "customer@example.com";
            var customerUser = await userManager.FindByEmailAsync(customerEmail);

            if (customerUser == null)
            {
                customerUser = new ApplicationUser
                {
                    UserName = customerEmail,
                    Email = customerEmail,
                    FirstName = "Анна",
                    LastName = "Петрова",
                    EmailConfirmed = true
                };

                var customerCreateResult = await userManager.CreateAsync(customerUser, "Customer123!");
                if (customerCreateResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(customerUser, "Customer");
                }
            }


            // Проверяем, есть ли данные в базе
            if (context.Products.Any())
            {
                return; // База уже заполнена
            }

            // Добавляем тестовые товары
            var products = new[]
            {
                new Product
                {
                    Name = "Элегантное платье миди",
                    Description = "Платье миди с кружевными вставками и завышенной талией. Идеально для офиса и свиданий.",
                    Price = 4990,
                    Category = "Платья",
                    Composition = "Хлопок 95%, Эластан 5%",
                    Article = "DRESS-001",
                    IsActive = true,
                    IsBestseller = true,
                    CreatedDate = DateTime.Now.AddDays(-10)
                },
                new Product
                {
                    Name = "Блузка с рюшами",
                    Description = "Блузка из шифона с кружевными рюшами. Нежный и женственный образ.",
                    Price = 3500,
                    Category = "Блузки",
                    Composition = "Шифон 100%",
                    Article = "BLOUSE-001",
                    IsActive = true,
                    IsBestseller = true,
                    CreatedDate = DateTime.Now.AddDays(-8)
                },
                new Product
                {
                    Name = "Юбка-карандаш",
                    Description = "Классическая юбка-карандаш для офиса. Подчеркивает фигуру.",
                    Price = 3200,
                    Category = "Юбки",
                    Composition = "Полиэстер 70%, Шерсть 30%",
                    Article = "SKIRT-001",
                    IsActive = true,
                    IsBestseller = false,
                    CreatedDate = DateTime.Now.AddDays(-5)
                },
                new Product
                {
                    Name = "Костюм офисный",
                    Description = "Строгий офисный костюм с юбкой и пиджаком. Качественная ткань.",
                    Price = 8900,
                    Category = "Костюмы",
                    Composition = "Шерсть 80%, Полиэстер 20%",
                    Article = "SUIT-001",
                    IsActive = true,
                    IsBestseller = true,
                    CreatedDate = DateTime.Now.AddDays(-3)
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // Добавляем изображения для товаров
            var productImages = new[]
            {
                new ProductImage { ProductId = 1, ImageUrl = "https://via.placeholder.com/600x800/ff6b8b/ffffff", DisplayOrder = 1 },
                new ProductImage { ProductId = 1, ImageUrl = "https://via.placeholder.com/600x800/e0a8ff/ffffff", DisplayOrder = 2 },
                new ProductImage { ProductId = 2, ImageUrl = "https://via.placeholder.com/600x800/88b04b/ffffff", DisplayOrder = 1 },
                new ProductImage { ProductId = 3, ImageUrl = "https://via.placeholder.com/600x800/6b5b95/ffffff", DisplayOrder = 1 },
                new ProductImage { ProductId = 4, ImageUrl = "https://via.placeholder.com/600x800/ff8e53/ffffff", DisplayOrder = 1 },
            };

            context.ProductImages.AddRange(productImages);
            await context.SaveChangesAsync();

            // Создаем тестового пользователя
            var user = new ApplicationUser
            {
                UserName = "test@example.com",
                Email = "test@example.com",
                FirstName = "Анна",
                LastName = "Петрова",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, "Test123!");
            if (result.Succeeded)
            {
                // Создаем тестовые заказы
                var order = new Order
                {
                    UserId = user.Id,
                    OrderDate = DateTime.Now.AddDays(-7),
                    TotalAmount = 8490,
                    Status = "Выполнен",
                    CustomerName = "Анна Петрова",
                    CustomerEmail = "test@example.com",
                    CustomerPhone = "+7 (999) 123-45-67",
                    ShippingAddress = "г. Москва, ул. Примерная, 10",
                    DeliveryMethod = "Курьерская доставка",
                    PaymentMethod = "Банковская карта"
                };

                context.Orders.Add(order);
                await context.SaveChangesAsync();

                // Добавляем элементы заказа
                var orderItems = new[]
                {
                    new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = 1,
                        Quantity = 1,
                        UnitPrice = 4990,
                        
                    },
                    new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = 2,
                        Quantity = 1,
                        UnitPrice = 3500,
                        
                    }
                };

                context.OrderItems.AddRange(orderItems);
                await context.SaveChangesAsync();

                // Добавляем тестовые отзывы
                var reviews = new[]
                {
                    new Review
                    {
                        ProductId = 1,
                        UserId = user.Id,
                        Rating = 5,
                        Comment = "Отличное платье! Сидит идеально, ткань приятная к телу.",
                        CreatedDate = DateTime.Now.AddDays(-5)
                    },
                    new Review
                    {
                        ProductId = 2,
                        UserId = user.Id,
                        Rating = 4,
                        Comment = "Хорошая блузка, но немного просвечивает.",
                        CreatedDate = DateTime.Now.AddDays(-3)
                    }
                };

                context.Reviews.AddRange(reviews);
                await context.SaveChangesAsync();
            }

            // Создаем администратора
            var admin = new ApplicationUser
            {
                UserName = "admin@alicedress.ru",
                Email = "admin@alicedress.ru",
                FirstName = "Администратор",
                LastName = "Системы",
                EmailConfirmed = true
            };

            var adminResult = await userManager.CreateAsync(admin, "Admin123!");
            if (adminResult.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}