using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using alicedress.Data;
using alicedress.Models;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация БД
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Сессии для корзины
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Для работы с файлами и загрузкой
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

var app = builder.Build();

// Конвейер middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // ВАЖНО: Должно быть до UseAuthentication
app.UseAuthentication();
app.UseAuthorization();

// Настройка маршрутов
app.UseEndpoints(endpoints =>
{
    // Маршрут для областей (админ-панель) - ВАЖНО: первый
    endpoints.MapAreaControllerRoute(
        name: "AdminArea",
        areaName: "Admin",
        pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

    // Маршрут для Identity
    endpoints.MapRazorPages();

    // Основной маршрут для обычных контроллеров
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});

// Альтернативный вариант (более простой):
// app.MapControllerRoute(
//     name: "areas",
//     pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// app.MapControllerRoute(
//     name: "default",
//     pattern: "{controller=Home}/{action=Index}/{id?}");

// app.MapRazorPages();

// Инициализация базы данных
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        // Создание ролей
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        if (!await roleManager.RoleExistsAsync("Customer"))
            await roleManager.CreateAsync(new IdentityRole("Customer"));

        // Создание администратора
        var adminEmail = "admin@alicedress.ru";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Администратор",
                LastName = "Системы",
                EmailConfirmed = true,
                RegistrationDate = DateTime.Now
            };

            var createResult = await userManager.CreateAsync(admin, "Admin123!");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        // Тестовые данные для товаров (если нет товаров)
        if (!context.Products.Any())
        {
            var products = new[]
            {
                new Product
                {
                    Name = "Элегантное платье миди",
                    Description = "Платье миди с кружевными вставками, идеально подходит для особых случаев",
                    Price = 4990,
                    Category = "Платья",
                    Composition = "Хлопок 95%, Эластан 5%",
                    Article = "DRESS-001",
                    IsActive = true,
                    IsBestseller = true,
                    StockQuantity = 50,
                    CreatedDate = DateTime.Now.AddDays(-10)
                },
                new Product
                {
                    Name = "Блузка с рюшами",
                    Description = "Блузка из шифона с кружевными рюшами, романтичный стиль",
                    Price = 3500,
                    Category = "Блузки",
                    Composition = "Шифон 100%",
                    Article = "BLOUSE-001",
                    IsActive = true,
                    IsBestseller = true,
                    StockQuantity = 30,
                    CreatedDate = DateTime.Now.AddDays(-8)
                },
                new Product
                {
                    Name = "Кожаная юбка-карандаш",
                    Description = "Классическая кожаная юбка-карандаш для офиса и вечера",
                    Price = 6500,
                    Category = "Юбки",
                    Composition = "Натуральная кожа 100%",
                    Article = "SKIRT-001",
                    IsActive = true,
                    IsBestseller = false,
                    StockQuantity = 25,
                    CreatedDate = DateTime.Now.AddDays(-5)
                }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            // Добавление изображений для тестовых товаров
            var random = new Random();
            var colors = new[] { "FF6B8B", "E0A8FF", "88B04B", "6A5ACD", "FFA500" };

            foreach (var product in products)
            {
                for (int i = 1; i <= 3; i++)
                {
                    var color = colors[random.Next(colors.Length)];
                    context.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = $"https://via.placeholder.com/600x800/{color}/FFFFFF?text={Uri.EscapeDataString(product.Name)}",
                        DisplayOrder = i
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка инициализации базы данных");
    }
}

app.Run();