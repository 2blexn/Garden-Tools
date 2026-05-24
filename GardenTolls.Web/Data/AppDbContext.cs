using GardenTolls.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GardenTolls.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionProduct> PromotionProducts => Set<PromotionProduct>();
    public DbSet<SearchQueryLog> SearchQueryLogs => Set<SearchQueryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>();

        modelBuilder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.Subcategories)
            .HasForeignKey(c => c.ParentCategoryID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>().HasIndex(p => p.SKU).IsUnique();
        modelBuilder.Entity<Product>().Property(p => p.UnitPrice).HasColumnType("decimal(10, 2)");

        modelBuilder.Entity<Customer>().HasIndex(c => c.Email).IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(u => u.Customer)
            .WithOne(c => c.User)
            .HasForeignKey<User>(u => u.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderDetail>().Property(od => od.UnitPrice).HasColumnType("decimal(10, 2)");
        modelBuilder.Entity<OrderDetail>().Property(od => od.Discount).HasColumnType("decimal(5, 2)");

        modelBuilder.Entity<Product>().HasIndex(p => p.CategoryID);
        modelBuilder.Entity<Product>().HasIndex(p => new { p.CategoryID, p.UnitPrice });
        modelBuilder.Entity<Order>().HasIndex(o => o.CustomerID);
        modelBuilder.Entity<Order>().HasIndex(o => new { o.Status, o.OrderDate });
        modelBuilder.Entity<Inventory>().HasIndex(i => i.QuantityInStock);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.ParentReview)
            .WithMany(r => r.Replies)
            .HasForeignKey(r => r.ParentReviewID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderDetail>().ToTable(tb => tb.HasTrigger("OrderDetailsTrigger"));
        modelBuilder.Entity<Order>().ToTable(tb => tb.HasTrigger("OrdersTrigger"));

        modelBuilder.Entity<PromotionProduct>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("tr_CalculatePromotionalPrice"));
            entity.Property(pp => pp.DiscountPercentage).HasColumnType("decimal(5, 2)");
        });
    }
}
