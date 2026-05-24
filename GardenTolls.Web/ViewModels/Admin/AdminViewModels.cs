using System.ComponentModel.DataAnnotations;
using GardenTolls.Web.Models;
using GardenTolls.Web.ViewModels.Products;

namespace GardenTolls.Web.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int ProductCount { get; set; }
    public int UserCount { get; set; }
    public int OrderCount { get; set; }
    public int PendingReviews { get; set; }
    public int LowStockCount { get; set; }
    public decimal RevenueLast30Days { get; set; }
}

public class AdminProductEditViewModel
{
    public int? ProductId { get; set; }

    [Required]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int SupplierId { get; set; }

    public string? Description { get; set; }

    [Required]
    [Range(0.01, 999999)]
    public decimal UnitPrice { get; set; }

    [Required]
    public string Sku { get; set; } = string.Empty;

    public string? ImageBase64 { get; set; }
    public bool IsDiscontinued { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; } = 5;
    public List<CategoryOption> Categories { get; set; } = [];
    public List<SupplierOption> Suppliers { get; set; } = [];
}

public class AdminUserViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfileImageBase64 { get; set; }
    public string AuthorInitial { get; set; } = "К";
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool CanWriteReviews { get; set; }
    public DateTime RegistrationDate { get; set; }
}

public class AdminReviewViewModel
{
    public int ReviewId { get; set; }
    public int UserId { get; set; }
    public int? ParentReviewId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
    public string AuthorInitial { get; set; } = "К";
    public byte? Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public bool UserCanWriteReviews { get; set; }
    public DateTime ReviewDate { get; set; }
    public int RiskScore { get; set; }
    public List<string> ModerationFlags { get; set; } = [];
    public bool IsHighRisk => RiskScore >= 70;
    public bool IsReply => ParentReviewId.HasValue;
}

public class AdminPromotionEditViewModel
{
    public int? PromotionId { get; set; }

    [Required]
    public string PromotionName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public int SupplierId { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);
    public bool IsActive { get; set; } = true;
    public List<SupplierOption> Suppliers { get; set; } = [];
    public List<AdminPromotionProductLine> ProductLines { get; set; } = [];
    public List<ProductOption> AvailableProducts { get; set; } = [];
    public List<CategoryOption> Categories { get; set; } = [];
    public List<(string Value, string Label)> SeasonOptions { get; set; } = [];
}

public class AdminPromotionProductLine
{
    public int? PromotionProductId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal ResultPrice => UnitPrice * (1 - DiscountPercentage / 100m);
    public bool IsActive { get; set; } = true;
}

public class ProductOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class AdminCategoryViewModel
{
    public int? CategoryId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public int ProductCount { get; set; }
    public List<CategoryOption> ParentCategories { get; set; } = [];
}

public class PromotionBulkAddViewModel
{
    public int PromotionId { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public string? Season { get; set; }
    public decimal DiscountPercent { get; set; } = 10;
}

public class AdminQuickDiscountViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; } = 10;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(7);
}

public class AdminPromotionViewModel
{
    public int PromotionId { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
}

public class AnalyticsViewModel
{
    public List<SalesPointViewModel> SalesByMonth { get; set; } = [];
    public List<PopularProductViewModel> PopularProducts { get; set; } = [];
    public CustomerStatsViewModel CustomerStats { get; set; } = new();
}

public class SalesPointViewModel
{
    public string Label { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

public class PopularProductViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class CustomerStatsViewModel
{
    public int TotalCustomers { get; set; }
    public int NewCustomersLast30Days { get; set; }
    public decimal AverageOrderValue { get; set; }
}
