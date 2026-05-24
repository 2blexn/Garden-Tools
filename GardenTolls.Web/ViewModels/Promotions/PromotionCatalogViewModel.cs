using GardenTolls.Web.ViewModels.Products;

namespace GardenTolls.Web.ViewModels.Promotions;

public class PromotionCatalogViewModel
{
    public int PromotionId { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal? MaxDiscountPercent { get; set; }
    public bool IsNightFlash { get; set; }
    public ProductFilterViewModel Filter { get; set; } = new();
    public List<PromotionProductCardViewModel> Products { get; set; } = [];
    public int TotalCount => Products.Count;
}

public class PromotionProductCardViewModel : ProductCardViewModel
{
    public decimal PromotionDiscountPercent { get; set; }
}
