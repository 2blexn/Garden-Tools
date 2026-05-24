namespace GardenTolls.Web.ViewModels.Cart;

public class CartItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageBase64 { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public int Quantity { get; set; }
    public int Stock { get; set; }
    public decimal EffectiveUnitPrice => UnitPrice * (1 - DiscountPercent / 100m);
    public decimal LineTotal => Quantity * EffectiveUnitPrice;
}

public class CartViewModel
{
    public List<CartItemViewModel> Items { get; set; } = [];
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public int TotalItems => Items.Sum(i => i.Quantity);
}

public class WishlistViewModel
{
    public List<GardenTolls.Web.ViewModels.Products.ProductCardViewModel> Items { get; set; } = [];
}
