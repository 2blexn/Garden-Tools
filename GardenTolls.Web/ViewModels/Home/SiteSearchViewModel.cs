using GardenTolls.Web.ViewModels.Products;
using GardenTolls.Web.ViewModels.Promotions;

namespace GardenTolls.Web.ViewModels.Home;

public class SiteSearchViewModel
{
    public string? Query { get; set; }
    public List<ProductCardViewModel> Products { get; set; } = [];
    public List<PromotionCardViewModel> Promotions { get; set; } = [];
    public SearchSuggestionsViewModel Suggestions { get; set; } = new();
    public int TotalResults => Products.Count + Promotions.Count;
}
