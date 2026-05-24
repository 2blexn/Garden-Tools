using GardenTolls.Web.Infrastructure;
using GardenTolls.Web.Services;
using GardenTolls.Web.ViewModels.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GardenTolls.Web.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    public async Task<IActionResult> Checkout()
    {
        var id = User.GetUserId();
        if (!id.HasValue) return Challenge();
        var model = await _orders.BuildCheckoutAsync(HttpContext.Session, id.Value);
        return model == null ? RedirectToAction("Index", "Cart") : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        var userId = User.GetUserId()!.Value;

        if (!ModelState.IsValid)
        {
            var rebuilt = await _orders.BuildCheckoutAsync(HttpContext.Session, userId);
            if (rebuilt != null)
            {
                model.Lines = rebuilt.Lines;
                model.TotalAmount = rebuilt.TotalAmount;
            }
            return View(model);
        }

        var (success, message, orderId) = await _orders.PlaceOrderAsync(model, HttpContext.Session, userId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            var rebuilt = await _orders.BuildCheckoutAsync(HttpContext.Session, userId);
            if (rebuilt != null)
            {
                model.Lines = rebuilt.Lines;
                model.TotalAmount = rebuilt.TotalAmount;
            }
            return View(model);
        }

        if (string.Equals(model.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Success), new { id = orderId });

        return RedirectToAction(nameof(Payment), new { id = orderId });
    }

    public async Task<IActionResult> Payment(int id)
    {
        var userId = User.GetUserId()!.Value;
        var order = await _orders.GetOrderDetailsAsync(id, userId, User.IsAdminOrManager());
        if (order == null) return NotFound();

        if (order.PaymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Success), new { id });

        return View(new PaymentViewModel { OrderId = id, Amount = order.TotalAmount });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Payment(PaymentViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, message) = await _orders.SimulatePaymentAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        return RedirectToAction(nameof(Success), new { id = model.OrderId });
    }

    public async Task<IActionResult> Success(int id)
    {
        var userId = User.GetUserId()!.Value;
        var model = await _orders.GetOrderDetailsAsync(id, userId, User.IsAdminOrManager());
        return model == null ? NotFound() : View(model);
    }

    public async Task<IActionResult> History()
    {
        var id = User.GetUserId();
        if (!id.HasValue) return Challenge();
        return View(await _orders.GetHistoryAsync(id.Value));
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = User.GetUserId()!.Value;
        var model = await _orders.GetOrderDetailsAsync(id, userId, User.IsAdminOrManager());
        return model == null ? NotFound() : View(model);
    }

    public async Task<IActionResult> Receipt(int id, bool download = false)
    {
        var userId = User.GetUserId()!.Value;
        var model = await _orders.GetOrderDetailsAsync(id, userId, User.IsAdminOrManager());
        if (model == null) return NotFound();

        if (download)
        {
            var html = BuildReceiptHtml(model);
            var bytes = System.Text.Encoding.UTF8.GetBytes(html);
            return File(bytes, "text/html; charset=utf-8", $"chek-zamovlennia-{id}.html");
        }

        return View(model);
    }

    private static string BuildReceiptHtml(OrderDetailsViewModel model)
    {
        var lines = string.Join("", model.Lines.Select(l =>
            $"<tr><td>{l.ProductName}</td><td>{l.Quantity}</td><td>{l.UnitPrice:N2}</td><td>{l.LineTotal:N2}</td></tr>"));
        return $@"<!DOCTYPE html><html lang=""uk""><head><meta charset=""utf-8""/><title>Чек #{model.OrderId}</title>
<style>body{{font-family:Segoe UI,sans-serif;padding:24px}}table{{width:100%;border-collapse:collapse}}td,th{{border:1px solid #ccc;padding:8px}}</style></head>
<body><h1>Чек замовлення #{model.OrderId}</h1>
<p>Дата: {model.OrderDate:dd.MM.yyyy HH:mm}</p>
<p>Клієнт: {model.CustomerName}</p>
<p>Оплата: {model.PaymentMethod} ({model.PaymentStatus})</p>
<table><thead><tr><th>Товар</th><th>К-сть</th><th>Ціна</th><th>Сума</th></tr></thead><tbody>{lines}</tbody></table>
<p><strong>Разом: {model.TotalAmount:N2} грн</strong></p></body></html>";
    }
}
