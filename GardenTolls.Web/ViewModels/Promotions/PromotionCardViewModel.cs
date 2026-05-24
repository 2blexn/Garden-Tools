namespace GardenTolls.Web.ViewModels.Promotions;

public class PromotionCardViewModel
{
    public int PromotionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? SupplierName { get; set; }
    public decimal? MaxDiscountPercent { get; set; }
    public int ProductCount { get; set; }
    public bool IsCurrentlyActive { get; set; }
    public bool IsNightFlash { get; set; }
}
