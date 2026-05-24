using GardenTolls.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GardenTolls.Web.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cart;

    public CartController(ICartService cart) => _cart = cart;

    public IActionResult Index() => View(_cart.GetCart(HttpContext.Session));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1, string? returnUrl = null)
    {
        var ok = await _cart.AddToCartAsync(HttpContext.Session, productId, quantity);
        TempData[ok ? "Success" : "Error"] = ok
            ? "Товар додано до кошика"
            : "Не вдалося додати товар (немає в наявності або товар не знайдено)";

        var redirect = GetSafeReturnUrl(returnUrl);
        if (redirect != null)
            return Redirect(redirect);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        _cart.ClearCart(HttpContext.Session);
        TempData["Success"] = "Кошик очищено";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int productId, int quantity)
    {
        await _cart.UpdateQuantityAsync(HttpContext.Session, productId, quantity);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId, string? returnUrl = null)
    {
        await _cart.RemoveFromCartAsync(HttpContext.Session, productId);
        TempData["Success"] = "Товар прибрано з кошика";

        var redirect = GetSafeReturnUrl(returnUrl);
        if (redirect != null)
            return Redirect(redirect);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCart(int productId, string? returnUrl = null)
    {
        var inCart = _cart.GetCartProductIds(HttpContext.Session).Contains(productId);
        if (inCart)
        {
            await _cart.RemoveFromCartAsync(HttpContext.Session, productId);
            TempData["Success"] = "Товар прибрано з кошика";
        }
        else
        {
            var ok = await _cart.AddToCartAsync(HttpContext.Session, productId, 1);
            TempData[ok ? "Success" : "Error"] = ok
                ? "Товар додано до кошика"
                : "Не вдалося додати товар (немає в наявності або товар не знайдено)";
        }

        var redirect = GetSafeReturnUrl(returnUrl);
        if (redirect != null)
            return Redirect(redirect);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Wishlist()
    {
        ViewBag.WishlistIds = _cart.GetWishlist(HttpContext.Session);
        return View(await _cart.GetWishlistPageAsync(HttpContext.Session));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleWishlist(int productId, string? returnUrl = null)
    {
        _cart.ToggleWishlist(HttpContext.Session, productId);
        var redirect = GetSafeReturnUrl(returnUrl);
        if (redirect != null)
            return Redirect(redirect);
        return RedirectToAction(nameof(Wishlist));
    }

    private string? GetSafeReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            if (returnUrl.StartsWith('/') && !returnUrl.StartsWith("//"))
                return returnUrl;
            if (Url.IsLocalUrl(returnUrl))
                return returnUrl;
        }

        var referer = Request.Headers.Referer.FirstOrDefault();
        if (string.IsNullOrEmpty(referer))
            return null;

        if (!Uri.TryCreate(referer, UriKind.Absolute, out var uri))
            return null;

        if (!string.Equals(uri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            return null;

        var path = uri.PathAndQuery;
        return path.StartsWith('/') ? path : null;
    }
}
