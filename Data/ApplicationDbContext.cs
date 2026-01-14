using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using alicedress.Models;

namespace alicedress.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Продукт -> Изображения (один ко многим)
            builder.Entity<Product>()
                .HasMany(p => p.Images)
                .WithOne(pi => pi.Product)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Продукт -> Отзывы (один ко многим)
            builder.Entity<Product>()
                .HasMany(p => p.Reviews)
                .WithOne(r => r.Product)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Заказ -> Позиции заказа (один ко многим)
            builder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Заказ -> Пользователь (многие к одному)
            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.SetNull); // При удалении пользователя заказы остаются

            // Позиция заказа -> Продукт (многие к одному)
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Запрещаем удаление продукта, если он есть в заказах

            // Отзыв -> Пользователь (многие к одному)
            builder.Entity<Review>()
                .HasOne(r => r.ApplicationUser)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Уникальные индексы для улучшения производительности
            builder.Entity<Product>()
                .HasIndex(p => p.Article)
                .IsUnique()
                .HasFilter("[Article] IS NOT NULL");

            builder.Entity<ProductImage>()
                .HasIndex(pi => new { pi.ProductId, pi.DisplayOrder })
                .IsUnique();

            builder.Entity<Order>()
                .HasIndex(o => o.OrderDate);

            builder.Entity<Order>()
                .HasIndex(o => o.Status);

            builder.Entity<Order>()
                .HasIndex(o => o.UserId);
        }
    }
}