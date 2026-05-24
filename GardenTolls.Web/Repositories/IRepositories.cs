using GardenTolls.Web.Models;
using GardenTolls.Web.ViewModels.Products;

namespace GardenTolls.Web.Repositories;

public interface IProductRepository
{
    Task<(List<Product> Items, int Total)> GetFilteredAsync(ProductFilterViewModel filter, CancellationToken ct = default);
    Task<Product?> GetByIdAsync(int id, bool includeReviews = false, CancellationToken ct = default);
    Task<List<Product>> GetRecommendationsAsync(int categoryId, int excludeId, int count = 4, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(int productId, CancellationToken ct = default);
}

public interface ICategoryRepository
{
    Task<List<Category>> GetActiveTreeAsync(CancellationToken ct = default);
    Task<List<Category>> GetAllActiveAsync(CancellationToken ct = default);
    Task<List<Category>> GetAllForAdminAsync(CancellationToken ct = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Category category, CancellationToken ct = default);
    Task UpdateAsync(Category category, CancellationToken ct = default);
    Task DeleteAsync(int categoryId, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<List<User>> GetAllAsync(CancellationToken ct = default);
}

public interface IOrderRepository
{
    Task<List<Order>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default);
    Task<List<Order>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
}

public interface IReviewRepository
{
    Task<List<Review>> GetApprovedForProductAsync(int productId, CancellationToken ct = default);
    Task<List<Review>> GetForProductAsync(int productId, CancellationToken ct = default);
    Task<List<Review>> GetApprovedWithRepliesForProductAsync(int productId, CancellationToken ct = default);
    Task<List<Review>> GetAllApprovedForProductAsync(int productId, CancellationToken ct = default);
    Task<List<Review>> GetAllForModerationAsync(CancellationToken ct = default);
    Task<List<Review>> GetPendingAsync(CancellationToken ct = default);
    Task AddAsync(Review review, CancellationToken ct = default);
    Task<Review?> GetByIdAsync(int id, CancellationToken ct = default);
    Task UpdateAsync(Review review, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IPromotionRepository
{
    Task<decimal?> GetActiveDiscountPercentAsync(int productId, CancellationToken ct = default);
    Task<List<Promotion>> GetActiveAsync(CancellationToken ct = default);
    Task<List<Promotion>> GetAllForCatalogAsync(CancellationToken ct = default);
    Task<Promotion?> GetByIdAsync(int promotionId, CancellationToken ct = default);
    Task<List<PromotionProduct>> GetProductsForPromotionAsync(int promotionId, CancellationToken ct = default);
}

public interface IAnalyticsRepository
{
    Task<List<(string Label, decimal Total)>> GetSalesByMonthAsync(int months, CancellationToken ct = default);
    Task<List<(string Name, int Qty, decimal Revenue)>> GetPopularProductsAsync(int top, CancellationToken ct = default);
    Task<(int Total, int New30, decimal AvgOrder)> GetCustomerStatsAsync(CancellationToken ct = default);
}

public interface ISupplierRepository
{
    Task<List<Supplier>> GetActiveAsync(CancellationToken ct = default);
}

public interface ICustomerRepository
{
    Task<Customer?> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
    Task UpdateAsync(Customer customer, CancellationToken ct = default);
}

public interface IInventoryRepository
{
    Task<Inventory?> GetByProductIdAsync(int productId, CancellationToken ct = default);
    Task UpsertAsync(int productId, int quantity, int reorderLevel, CancellationToken ct = default);
}
