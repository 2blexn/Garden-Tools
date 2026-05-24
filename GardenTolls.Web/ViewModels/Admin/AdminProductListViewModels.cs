using GardenTolls.Web.ViewModels.Products;

namespace GardenTolls.Web.ViewModels.Admin;

public class AdminProductFilterViewModel
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; }
}

public class AdminProductRowViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int QuantityInStock { get; set; }
    public string? ImageBase64 { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
}

public class AdminProductsPageViewModel
{
    public AdminProductFilterViewModel Filter { get; set; } = new();
    public List<AdminProductRowViewModel> Products { get; set; } = [];
    public List<CategoryOption> Categories { get; set; } = [];
    public List<SupplierOption> Suppliers { get; set; } = [];
    public int TotalCount => Products.Count;
}
