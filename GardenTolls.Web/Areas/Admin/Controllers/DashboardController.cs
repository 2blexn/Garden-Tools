using GardenTolls.Web.Models;
using GardenTolls.Web.Services;
using GardenTolls.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GardenTolls.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOrManager")]
public class DashboardController : Controller
{
    private readonly IAdminService _admin;
    private readonly IAnalyticsService _analytics;
    private readonly IReportExportService _reports;

    public DashboardController(IAdminService admin, IAnalyticsService analytics, IReportExportService reports)
    {
        _admin = admin;
        _analytics = analytics;
        _reports = reports;
    }

    public async Task<IActionResult> Index() => View(await _admin.GetDashboardAsync());

    public async Task<IActionResult> Products(AdminProductFilterViewModel filter) =>
        View(await _admin.GetProductsAdminPageAsync(filter));

    public async Task<IActionResult> EditProduct(int? id) =>
        View(await _admin.GetProductForEditAsync(id));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(AdminProductEditViewModel model, IFormFile? productImage)
    {
        if (productImage != null && productImage.Length > 0)
        {
            if (productImage.Length > 512_000)
            {
                ModelState.AddModelError(string.Empty, "Зображення занадто велике (макс. 500 КБ)");
            }
            else
            {
                await using var ms = new MemoryStream();
                await productImage.CopyToAsync(ms);
                model.ImageBase64 = Convert.ToBase64String(ms.ToArray());
            }
        }

        if (!ModelState.IsValid)
        {
            model = await _admin.GetProductForEditAsync(model.ProductId) ?? model;
            return View(model);
        }
        await _admin.SaveProductAsync(model);
        TempData["Success"] = "Збережено";
        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        await _admin.DeleteProductAsync(id);
        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDiscount(AdminQuickDiscountViewModel model)
    {
        await _admin.ApplyQuickDiscountAsync(model);
        TempData["Success"] = "Знижку застосовано";
        return RedirectToAction(nameof(Products));
    }

    public async Task<IActionResult> Users() => View(await _admin.GetUsersAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetUserActive(int userId, bool isActive)
    {
        await _admin.SetUserActiveAsync(userId, isActive);
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetUserCanWriteReviews(int userId, bool canWrite)
    {
        await _admin.SetUserCanWriteReviewsAsync(userId, canWrite);
        TempData["Success"] = canWrite ? "Право на відгуки відновлено" : "Користувачу заборонено писати відгуки";
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Orders() => View(await _admin.GetOrdersAdminAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
    {
        await _admin.UpdateOrderStatusAsync(orderId, status);
        return RedirectToAction(nameof(Orders));
    }

    public async Task<IActionResult> Reviews() => View(await _admin.GetReviewsAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveReview(int reviewId, bool approved)
    {
        await _admin.ApproveReviewAsync(reviewId, approved);
        return RedirectToAction(nameof(Reviews));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HideReview(int reviewId)
    {
        await _admin.HideReviewAsync(reviewId);
        TempData["Success"] = "Відгук приховано";
        return RedirectToAction(nameof(Reviews));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReview(int reviewId)
    {
        await _admin.DeleteReviewAsync(reviewId);
        return RedirectToAction(nameof(Reviews));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReviewAndRestrict(int reviewId)
    {
        await _admin.DeleteReviewAndRestrictUserAsync(reviewId);
        TempData["Success"] = "Відгук видалено, користувачу обмежено відгуки";
        return RedirectToAction(nameof(Reviews));
    }

    public async Task<IActionResult> Promotions() => View(await _admin.GetPromotionsAsync());

    public async Task<IActionResult> EditPromotion(int? id) =>
        View(await _admin.GetPromotionForEditAsync(id));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPromotion(AdminPromotionEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await _admin.GetPromotionForEditAsync(model.PromotionId) ?? model;
            return View(model);
        }
        await _admin.SavePromotionAsync(model);
        TempData["Success"] = "Акцію збережено";
        return RedirectToAction(nameof(Promotions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnablePromotion(int promotionId)
    {
        await _admin.EnablePromotionAsync(promotionId);
        TempData["Success"] = "Акцію увімкнено";
        return RedirectToAction(nameof(Promotions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisablePromotion(int promotionId)
    {
        await _admin.DisablePromotionAsync(promotionId);
        TempData["Success"] = "Акцію вимкнено";
        return RedirectToAction(nameof(Promotions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePromotionProduct(int promotionProductId, int promotionId)
    {
        await _admin.RemovePromotionProductAsync(promotionProductId);
        TempData["Success"] = "Товар прибрано з акції";
        return RedirectToAction(nameof(EditPromotion), new { id = promotionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAddPromotionProducts(PromotionBulkAddViewModel model)
    {
        var added = await _admin.AddProductsToPromotionByFilterAsync(
            model.PromotionId, model.CategoryId, model.SupplierId, model.Season, model.DiscountPercent);
        TempData["Success"] = added > 0 ? $"Додано товарів: {added}" : "Нових товарів за фільтром не знайдено";
        return RedirectToAction(nameof(EditPromotion), new { id = model.PromotionId });
    }

    public async Task<IActionResult> Categories() => View(await _admin.GetCategoriesAdminAsync());

    public async Task<IActionResult> EditCategory(int? id) =>
        View(await _admin.GetCategoryForEditAsync(id));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(AdminCategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await _admin.GetCategoryForEditAsync(model.CategoryId) ?? model;
            return View(model);
        }
        await _admin.SaveCategoryAsync(model);
        TempData["Success"] = "Категорію збережено";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await _admin.DeleteCategoryAsync(id);
        TempData["Success"] = "Категорію видалено або приховано";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ShowCategory(int categoryId)
    {
        await _admin.SetCategoryActiveAsync(categoryId, true);
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HideCategory(int categoryId)
    {
        await _admin.SetCategoryActiveAsync(categoryId, false);
        return RedirectToAction(nameof(Categories));
    }

    public async Task<IActionResult> Analytics() => View(await _analytics.GetAnalyticsAsync());

    public async Task<IActionResult> ExportReport(string report, string format = "html")
    {
        var data = await _analytics.GetAnalyticsAsync();
        var html = report?.ToLowerInvariant() switch
        {
            "sales" => _reports.BuildSalesReportHtml(data),
            "popular" => _reports.BuildPopularProductsReportHtml(data),
            "customers" => _reports.BuildCustomersReportHtml(data),
            _ => _reports.BuildSalesReportHtml(data)
        };

        var fileName = $"garden-tools-{report}-{DateTime.Now:yyyyMMdd}.html";
        var bytes = System.Text.Encoding.UTF8.GetBytes(html);

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            return File(bytes, "text/html; charset=utf-8", fileName.Replace(".html", "-print.html"));
        }

        return File(bytes, "text/html; charset=utf-8", fileName);
    }

    public async Task<IActionResult> PreviewReport(string report)
    {
        var data = await _analytics.GetAnalyticsAsync();
        var html = report?.ToLowerInvariant() switch
        {
            "sales" => _reports.BuildSalesReportHtml(data),
            "popular" => _reports.BuildPopularProductsReportHtml(data),
            "customers" => _reports.BuildCustomersReportHtml(data),
            _ => _reports.BuildSalesReportHtml(data)
        };
        return Content(html, "text/html; charset=utf-8");
    }
}
