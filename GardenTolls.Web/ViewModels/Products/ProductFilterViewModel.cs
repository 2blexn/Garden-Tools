namespace GardenTolls.Web.ViewModels.Products;

public class ProductFilterViewModel
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public string? Season { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool InStockOnly { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class ProductListViewModel
{
    public ProductFilterViewModel Filter { get; set; } = new();
    public List<ProductCardViewModel> Products { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Math.Max(Filter.PageSize, 1));
    public List<CategoryOption> Categories { get; set; } = [];
    public List<SupplierOption> Suppliers { get; set; } = [];
}

public class ProductCardViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public double? AverageRating { get; set; }
    public string? ImageBase64 { get; set; }
    /// <summary>-1 = немає запису складу (вважаємо в наявності).</summary>
    public int Stock { get; set; }
    public bool CanAddToCart => Stock != 0;
    public decimal DisplayPrice => DiscountedPrice ?? UnitPrice;
}

public class CategoryOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SupplierOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ProductDetailsViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal? Weight { get; set; }
    public string? Dimensions { get; set; }
    public string? ImageBase64 { get; set; }
    public int Stock { get; set; }
    public bool CanAddToCart => Stock != 0;
    public double? AverageRating { get; set; }
    public decimal DisplayPrice => DiscountedPrice ?? UnitPrice;
    public List<ReviewViewModel> Reviews { get; set; } = [];
    public List<ProductCardViewModel> Recommendations { get; set; } = [];
    public ReviewInputViewModel NewReview { get; set; } = new();
    public int? FromPromotionId { get; set; }
    public string? FromPromotionName { get; set; }
}

public class ReviewViewModel
{
    public int ReviewId { get; set; }
    public int UserId { get; set; }
    public string Author { get; set; } = string.Empty;
    public string AuthorInitial { get; set; } = "К";
    public byte? Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime ReviewDate { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public string? ProfileImageBase64 { get; set; }
    public List<ReviewViewModel> Replies { get; set; } = [];
}

public class ReviewInputViewModel
{
    public byte Rating { get; set; } = 5;
    public string? Comment { get; set; }
    public int? ParentReviewId { get; set; }
}
