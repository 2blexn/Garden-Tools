using GardenTolls.Web.Services;
using GardenTolls.Web.ViewModels.Products;
using Microsoft.AspNetCore.Mvc;

namespace GardenTolls.Web.Controllers;

public class PromotionsController : Controller
{
    private readonly ICatalogService _catalog;
    private readonly ICartService _cart;

    public PromotionsController(ICatalogService catalog, ICartService cart)
    {
        _catalog = catalog;
        _cart = cart;
    }

    public async Task<IActionResult> Details(int id, ProductFilterViewModel filter, string? sortOption = null)
    {
        if (!string.IsNullOrEmpty(sortOption))
        {
            var idx = sortOption.LastIndexOf('-');
            if (idx > 0)
            {
                filter.SortBy = sortOption[..idx];
                filter.SortDesc = sortOption[(idx + 1)..].Equals("desc", StringComparison.OrdinalIgnoreCase);
            }
        }

        var model = await _catalog.GetPromotionCatalogAsync(id, filter);
        if (model == null) return NotFound();

        ViewBag.WishlistIds = _cart.GetWishlist(HttpContext.Session);
        ViewBag.PromoId = id;
        return View(model);
    }
}
