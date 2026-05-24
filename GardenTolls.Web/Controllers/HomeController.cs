using GardenTolls.Web.Infrastructure;
using GardenTolls.Web.Repositories;
using GardenTolls.Web.Services;
using GardenTolls.Web.ViewModels.Home;
using GardenTolls.Web.ViewModels.Products;
using GardenTolls.Web.ViewModels.Promotions;
using Microsoft.AspNetCore.Mvc;

namespace GardenTolls.Web.Controllers;

public class HomeController : Controller
{
    private readonly ICatalogService _catalog;
    private readonly ICategoryRepository _categories;
    private readonly IPromotionRepository _promotions;
    private readonly ISearchService _search;

    public HomeController(
        ICatalogService catalog,
        ICategoryRepository categories,
        IPromotionRepository promotions,
        ISearchService search)
    {
        _catalog = catalog;
        _categories = categories;
        _promotions = promotions;
        _search = search;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Categories = await _categories.GetActiveTreeAsync();
        var featured = await _catalog.GetCatalogAsync(new ProductFilterViewModel { PageSize = 8, SortBy = "name" });
        ViewBag.Featured = featured.Products;

        var now = DateTime.UtcNow;
        var promos = await _promotions.GetAllForCatalogAsync();
        ViewBag.ActivePromotions = promos
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .Take(3)
            .Select(p => new PromotionCardViewModel
            {
                PromotionId = p.PromotionID,
                Name = p.PromotionName,
                Description = p.Description,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                MaxDiscountPercent = p.PromotionProducts.Where(pp => pp.IsActive).Select(pp => pp.DiscountPercentage).DefaultIfEmpty(0).Max(),
                IsCurrentlyActive = true,
                IsNightFlash = p.PromotionName.Contains("Нічна", StringComparison.OrdinalIgnoreCase)
            }).ToList();

        return View();
    }

    public async Task<IActionResult> SearchSuggestions(string? q)
    {
        var vm = await _search.GetSuggestionsAsync(User.GetUserId(), q);
        return PartialView("_SearchSuggestionsContent", vm);
    }

    public IActionResult Contacts() => View();

    public async Task<IActionResult> Search(string? q)
    {
        var query = q?.Trim();
        var vm = new SiteSearchViewModel
        {
            Query = query,
            Suggestions = await _search.GetSuggestionsAsync(User.GetUserId(), query)
        };
        if (string.IsNullOrWhiteSpace(query))
            return View(vm);

        await _search.LogSearchAsync(query, User.GetUserId());

        var catalog = await _catalog.GetCatalogAsync(new ProductFilterViewModel
        {
            Search = query,
            PageSize = 24,
            SortBy = "name"
        });
        vm.Products = catalog.Products;

        var now = DateTime.UtcNow;
        var promos = await _promotions.GetAllForCatalogAsync();
        vm.Promotions = promos
            .Where(p =>
                p.PromotionName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (p.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(p =>
            {
                var activeProducts = p.PromotionProducts.Where(pp => pp.IsActive).ToList();
                return new PromotionCardViewModel
                {
                    PromotionId = p.PromotionID,
                    Name = p.PromotionName,
                    Description = p.Description,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    SupplierName = p.Supplier?.CompanyName,
                    MaxDiscountPercent = activeProducts.Count > 0 ? activeProducts.Max(pp => pp.DiscountPercentage) : null,
                    ProductCount = activeProducts.Count,
                    IsCurrentlyActive = p.StartDate <= now && p.EndDate >= now,
                    IsNightFlash = p.PromotionName.Contains("Нічна", StringComparison.OrdinalIgnoreCase)
                };
            }).ToList();

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Contacts(string name, string phone, string? email, string? message)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
        {
            TempData["Error"] = "Заповніть обов'язкові поля";
            return RedirectToAction(nameof(Contacts));
        }
        TempData["Success"] = "Дякуємо! Ваша заявка прийнята.";
        return RedirectToAction(nameof(Contacts));
    }

    public async Task<IActionResult> Promotions()
    {
        var now = DateTime.UtcNow;
        var promos = await _promotions.GetAllForCatalogAsync();
        var model = promos.Select(p =>
        {
            var activeProducts = p.PromotionProducts.Where(pp => pp.IsActive).ToList();
            return new PromotionCardViewModel
            {
                PromotionId = p.PromotionID,
                Name = p.PromotionName,
                Description = p.Description,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                SupplierName = p.Supplier?.CompanyName,
                MaxDiscountPercent = activeProducts.Count > 0
                    ? activeProducts.Max(pp => pp.DiscountPercentage)
                    : null,
                ProductCount = activeProducts.Count,
                IsCurrentlyActive = p.StartDate <= now && p.EndDate >= now,
                IsNightFlash = p.PromotionName.Contains("Нічна", StringComparison.OrdinalIgnoreCase)
            };
        }).ToList();
        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
