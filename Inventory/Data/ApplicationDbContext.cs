using Microsoft.EntityFrameworkCore;
using Inventory.Models;

namespace Inventory.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<StockIn> StockIns { get; set; }
        public DbSet<StockOut> StockOuts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product - Supplier relationship (explicitly define)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product - Category relationship
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // StockIn - Product relationship
            modelBuilder.Entity<StockIn>()
                .HasOne(s => s.Product)
                .WithMany(p => p.StockIns)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // StockIn - Supplier relationship
            modelBuilder.Entity<StockIn>()
                .HasOne(s => s.Supplier)
                .WithMany()
                .HasForeignKey(s => s.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // StockOut - Product relationship
            modelBuilder.Entity<StockOut>()
                .HasOne(s => s.Product)
                .WithMany(p => p.StockOuts)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            //// Fix HasData issue - Remove dynamic values
            //modelBuilder.Entity<User>().HasData(
            //    new User { Id = 1, UserName = "admin", Email = "admin@inventory.com", Password = "admin123", Role = "Admin" },
            //    new User { Id = 2, UserName = "manager", Email = "manager@inventory.com", Password = "manager123", Role = "Manager" },
            //    new User { Id = 3, UserName = "staff", Email = "staff@inventory.com", Password = "staff123", Role = "Staff" }
            //);

            modelBuilder.Entity<Product>()
        .Property(p => p.PurchasePrice)
        .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.SalePrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<StockIn>()
                .Property(s => s.UnitPrice)
                .HasPrecision(18, 2);

        }
    }
}