using GardenTolls.Web.Infrastructure;
using GardenTolls.Web.Services;
using GardenTolls.Web.ViewModels.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GardenTolls.Web.Controllers;

public class ProductsController : Controller
{
    private readonly ICatalogService _catalog;
    private readonly ICartService _cart;

    public ProductsController(ICatalogService catalog, ICartService cart)
    {
        _catalog = catalog;
        _cart = cart;
    }

    public async Task<IActionResult> Index(ProductFilterViewModel filter, string? sortOption = null)
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

        filter.PageSize = 9;
        ViewBag.WishlistIds = _cart.GetWishlist(HttpContext.Session);
        ViewBag.CartProductIds = _cart.GetCartProductIds(HttpContext.Session);
        return View(await _catalog.GetCatalogAsync(filter));
    }

    public async Task<IActionResult> Details(int id, int? promoId)
    {
        var model = await _catalog.GetProductDetailsAsync(id, promoId);
        return model == null ? NotFound() : View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(int id, ReviewInputViewModel newReview, int? promoId)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue) return Challenge();

        if (string.IsNullOrWhiteSpace(newReview.Comment))
        {
            TempData["Error"] = "Введіть текст відгуку або відповіді";
            return RedirectToAction(nameof(Details), new { id, promoId });
        }

        try
        {
            var message = await _catalog.AddReviewAsync(id, userId.Value, newReview);
            TempData["Success"] = message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id, promoId });
    }
}
