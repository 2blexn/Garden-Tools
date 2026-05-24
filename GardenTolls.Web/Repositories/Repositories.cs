using GardenTolls.Web.Data;
using GardenTolls.Web.Infrastructure;
using GardenTolls.Web.Models;
using GardenTolls.Web.ViewModels.Products;
using Microsoft.EntityFrameworkCore;

namespace GardenTolls.Web.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db) => _db = db;

    public async Task<(List<Product> Items, int Total)> GetFilteredAsync(ProductFilterViewModel filter, CancellationToken ct = default)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Inventory)
            .Include(p => p.Reviews)
            .Where(p => !p.IsDiscontinued)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(p =>
                p.ProductName.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)) ||
                p.SKU.ToLower().Contains(term));
        }

        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryID == filter.CategoryId);

        if (filter.SupplierId.HasValue)
            query = query.Where(p => p.SupplierID == filter.SupplierId);

        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.UnitPrice >= filter.MinPrice);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.UnitPrice <= filter.MaxPrice);

        if (filter.InStockOnly)
            query = query.Where(p => p.Inventory != null && p.Inventory.QuantityInStock > 0);

        if (!string.IsNullOrWhiteSpace(filter.Season) && SeasonFilter.Keywords.TryGetValue(filter.Season, out var keywords))
        {
            query = query.Where(p => keywords.Any(k =>
                p.ProductName.ToLower().Contains(k) ||
                (p.Description != null && p.Description.ToLower().Contains(k)) ||
                (p.Category != null && p.Category.Name.ToLower().Contains(k))));
        }

        query = filter.SortBy?.ToLower() switch
        {
            "price" => filter.SortDesc ? query.OrderByDescending(p => p.UnitPrice) : query.OrderBy(p => p.UnitPrice),
            _ => filter.SortDesc ? query.OrderByDescending(p => p.ProductName) : query.OrderBy(p => p.ProductName)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((Math.Max(filter.Page, 1) - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<Product?> GetByIdAsync(int id, bool includeReviews = false, CancellationToken ct = default)
    {
        var q = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Inventory)
            .AsQueryable();

        if (includeReviews)
            q = q.Include(p => p.Reviews.Where(r => r.IsApproved && r.ParentReviewID == null))
                 .ThenInclude(r => r.User);

        return q.FirstOrDefaultAsync(p => p.ProductID == id, ct);
    }

    public Task<List<Product>> GetRecommendationsAsync(int categoryId, int excludeId, int count = 4, CancellationToken ct = default) =>
        _db.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Inventory)
            .Include(p => p.Reviews)
            .Where(p => p.CategoryID == categoryId && p.ProductID != excludeId && !p.IsDiscontinued)
            .OrderByDescending(p => p.Reviews.Count)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        _db.Products.Update(product);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int productId, CancellationToken ct = default)
    {
        var p = await _db.Products.FindAsync([productId], ct);
        if (p != null)
        {
            p.IsDiscontinued = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;
    public CategoryRepository(AppDbContext db) => _db = db;

    public Task<List<Category>> GetActiveTreeAsync(CancellationToken ct = default) =>
        _db.Categories
            .Include(c => c.Subcategories)
            .Where(c => c.IsActive && c.ParentCategoryID == null)
            .AsNoTracking()
            .ToListAsync(ct);

    public Task<List<Category>> GetAllActiveAsync(CancellationToken ct = default) =>
        _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).AsNoTracking().ToListAsync(ct);

    public Task<List<Category>> GetAllForAdminAsync(CancellationToken ct = default) =>
        _db.Categories.OrderBy(c => c.Name).AsNoTracking().ToListAsync(ct);

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Categories.FirstOrDefaultAsync(c => c.CategoryID == id, ct);

    public async Task AddAsync(Category category, CancellationToken ct = default)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        _db.Categories.Update(category);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int categoryId, CancellationToken ct = default)
    {
        var c = await _db.Categories.FindAsync([categoryId], ct);
        if (c != null)
        {
            _db.Categories.Remove(c);
            await _db.SaveChangesAsync(ct);
        }
    }
}

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.UserId == id, ct);

    public Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken ct = default) =>
        _db.Users.AnyAsync(u => u.Email == email && (!excludeUserId.HasValue || u.UserId != excludeUserId), ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<User>> GetAllAsync(CancellationToken ct = default) =>
        _db.Users.OrderByDescending(u => u.RegistrationDate).AsNoTracking().ToListAsync(ct);
}

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;
    public OrderRepository(AppDbContext db) => _db = db;

    public Task<List<Order>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default) =>
        _db.Orders
            .Include(o => o.OrderDetails)
            .Where(o => o.CustomerID == customerId)
            .OrderByDescending(o => o.OrderDate)
            .AsNoTracking()
            .ToListAsync(ct);

    public Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default) =>
        _db.Orders
            .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.OrderID == orderId, ct);

    public Task<List<Order>> GetAllAsync(CancellationToken ct = default) =>
        _db.Orders.Include(o => o.Customer).Include(o => o.OrderDetails)
            .OrderByDescending(o => o.OrderDate).AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        _db.Orders.Update(order);
        await _db.SaveChangesAsync(ct);
    }
}

public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _db;
    public ReviewRepository(AppDbContext db) => _db = db;

    public Task<List<Review>> GetApprovedForProductAsync(int productId, CancellationToken ct = default) =>
        _db.Reviews.Include(r => r.User)
            .Where(r => r.ProductID == productId && r.IsApproved && r.ParentReviewID == null)
            .OrderByDescending(r => r.ReviewDate).AsNoTracking().ToListAsync(ct);

    public Task<List<Review>> GetForProductAsync(int productId, CancellationToken ct = default) =>
        _db.Reviews.Include(r => r.User)
            .Where(r => r.ProductID == productId && r.ParentReviewID == null)
            .OrderByDescending(r => r.ReviewDate).AsNoTracking().ToListAsync(ct);

    public Task<List<Review>> GetApprovedWithRepliesForProductAsync(int productId, CancellationToken ct = default) =>
        GetAllApprovedForProductAsync(productId, ct);

    public Task<List<Review>> GetAllApprovedForProductAsync(int productId, CancellationToken ct = default) =>
        _db.Reviews.Include(r => r.User)
            .Where(r => r.ProductID == productId && r.IsApproved)
            .OrderBy(r => r.ReviewDate)
            .AsNoTracking().ToListAsync(ct);

    public Task<List<Review>> GetAllForModerationAsync(CancellationToken ct = default) =>
        _db.Reviews.Include(r => r.Product).Include(r => r.User)
            .OrderByDescending(r => r.ReviewDate)
            .Take(200)
            .AsNoTracking()
            .ToListAsync(ct);

    public Task<List<Review>> GetPendingAsync(CancellationToken ct = default) =>
        _db.Reviews.Include(r => r.Product).Include(r => r.User)
            .Where(r => !r.IsApproved && r.ParentReviewID == null)
            .OrderByDescending(r => r.ReviewDate).AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(Review review, CancellationToken ct = default)
    {
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(ct);
    }

    public Task<Review?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Reviews.FindAsync([id], ct).AsTask();

    public async Task UpdateAsync(Review review, CancellationToken ct = default)
    {
        _db.Reviews.Update(review);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var r = await _db.Reviews.FindAsync([id], ct);
        if (r != null)
        {
            _db.Reviews.Remove(r);
            await _db.SaveChangesAsync(ct);
        }
    }
}

public class PromotionRepository : IPromotionRepository
{
    private readonly AppDbContext _db;
    public PromotionRepository(AppDbContext db) => _db = db;

    public async Task<decimal?> GetActiveDiscountPercentAsync(int productId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var discount = await _db.PromotionProducts
            .Include(pp => pp.Promotion)
            .Where(pp => pp.ProductID == productId && pp.IsActive &&
                         pp.Promotion != null && pp.Promotion.IsActive &&
                         pp.Promotion.StartDate <= now && pp.Promotion.EndDate >= now)
            .OrderByDescending(pp => pp.DiscountPercentage)
            .Select(pp => (decimal?)pp.DiscountPercentage)
            .FirstOrDefaultAsync(ct);
        return discount;
    }

    public Task<List<Promotion>> GetActiveAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return _db.Promotions.Include(p => p.Supplier)
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .AsNoTracking().ToListAsync(ct);
    }

    public Task<List<Promotion>> GetAllForCatalogAsync(CancellationToken ct = default) =>
        _db.Promotions
            .Include(p => p.Supplier)
            .Include(p => p.PromotionProducts)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.EndDate)
            .AsNoTracking()
            .ToListAsync(ct);

    public Task<Promotion?> GetByIdAsync(int promotionId, CancellationToken ct = default) =>
        _db.Promotions.Include(p => p.Supplier).AsNoTracking()
            .FirstOrDefaultAsync(p => p.PromotionID == promotionId, ct);

    public Task<List<PromotionProduct>> GetProductsForPromotionAsync(int promotionId, CancellationToken ct = default) =>
        _db.PromotionProducts.Include(pp => pp.Product).ThenInclude(p => p!.Category)
            .Include(pp => pp.Product).ThenInclude(p => p!.Supplier)
            .Include(pp => pp.Product).ThenInclude(p => p!.Inventory)
            .Include(pp => pp.Product).ThenInclude(p => p!.Reviews)
            .Where(pp => pp.PromotionID == promotionId && pp.IsActive)
            .AsNoTracking().ToListAsync(ct);
}

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly AppDbContext _db;
    public AnalyticsRepository(AppDbContext db) => _db = db;

    public async Task<List<(string Label, decimal Total)>> GetSalesByMonthAsync(int months, CancellationToken ct = default)
    {
        var from = DateTime.UtcNow.AddMonths(-months);
        var data = await _db.Orders
            .Where(o => o.OrderDate >= from && o.PaymentStatus == "Paid")
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(o => o.TotalAmount) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(ct);

        return data.Select(x => ($"{x.Month:00}/{x.Year}", x.Total)).ToList();
    }

    public async Task<List<(string Name, int Qty, decimal Revenue)>> GetPopularProductsAsync(int top, CancellationToken ct = default)
    {
        var rows = await _db.OrderDetails
            .Include(od => od.Product)
            .AsNoTracking()
            .ToListAsync(ct);

        return rows
            .GroupBy(od => od.Product?.ProductName ?? "Невідомий товар")
            .Select(g => (
                Name: g.Key,
                Qty: g.Sum(x => x.Quantity),
                Revenue: g.Sum(x => x.Quantity * x.UnitPrice * (1 - x.Discount / 100m))))
            .OrderByDescending(x => x.Qty)
            .Take(top)
            .ToList();
    }

    public async Task<(int Total, int New30, decimal AvgOrder)> GetCustomerStatsAsync(CancellationToken ct = default)
    {
        var total = await _db.Customers.CountAsync(ct);
        var from = DateTime.UtcNow.AddDays(-30);
        var new30 = await _db.Customers.CountAsync(c => c.RegistrationDate >= from, ct);
        var avg = await _db.Orders.Where(o => o.PaymentStatus == "Paid").AverageAsync(o => (decimal?)o.TotalAmount, ct) ?? 0;
        return (total, new30, avg);
    }
}

public class SupplierRepository : ISupplierRepository
{
    private readonly AppDbContext _db;
    public SupplierRepository(AppDbContext db) => _db = db;
    public Task<List<Supplier>> GetActiveAsync(CancellationToken ct = default) =>
        _db.Suppliers.Where(s => s.IsActive).OrderBy(s => s.CompanyName).AsNoTracking().ToListAsync(ct);
}

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;
    public CustomerRepository(AppDbContext db) => _db = db;

    public Task<Customer?> GetByUserIdAsync(int userId, CancellationToken ct = default) =>
        _db.Customers.Include(c => c.User).FirstOrDefaultAsync(c => c.User != null && c.User.UserId == userId, ct);

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Customers.FirstOrDefaultAsync(c => c.Email == email, ct);

    public async Task AddAsync(Customer customer, CancellationToken ct = default)
    {
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        _db.Customers.Update(customer);
        await _db.SaveChangesAsync(ct);
    }
}

public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _db;
    public InventoryRepository(AppDbContext db) => _db = db;

    public Task<Inventory?> GetByProductIdAsync(int productId, CancellationToken ct = default) =>
        _db.Inventories.FirstOrDefaultAsync(i => i.ProductID == productId, ct);

    public async Task UpsertAsync(int productId, int quantity, int reorderLevel, CancellationToken ct = default)
    {
        var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.ProductID == productId, ct);
        if (inv == null)
        {
            _db.Inventories.Add(new Inventory
            {
                ProductID = productId,
                QuantityInStock = quantity,
                ReorderLevel = reorderLevel,
                WarehouseLocation = "Main",
                LastRestocked = DateTime.UtcNow
            });
        }
        else
        {
            inv.QuantityInStock = quantity;
            inv.ReorderLevel = reorderLevel;
            inv.LastRestocked = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }
}
