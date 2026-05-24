using System.Text;
using GardenTolls.Web.ViewModels.Admin;

namespace GardenTolls.Web.Services;

public interface IReportExportService
{
    string BuildSalesReportHtml(AnalyticsViewModel data);
    string BuildPopularProductsReportHtml(AnalyticsViewModel data);
    string BuildCustomersReportHtml(AnalyticsViewModel data);
}

public class ReportExportService : IReportExportService
{
    public string BuildSalesReportHtml(AnalyticsViewModel data) =>
        WrapReport("Звіт: продажі по місяцях", BuildTable(
            ["Період", "Сума, грн"],
            data.SalesByMonth.Select(s => new[] { s.Label, s.Total.ToString("N2") })));

    public string BuildPopularProductsReportHtml(AnalyticsViewModel data) =>
        WrapReport("Звіт: популярні товари", BuildTable(
            ["Товар", "Продано, шт", "Дохід, грн"],
            data.PopularProducts.Select(p => new[] { p.ProductName, p.QuantitySold.ToString(), p.Revenue.ToString("N2") })));

    public string BuildCustomersReportHtml(AnalyticsViewModel data) =>
        WrapReport("Звіт: статистика клієнтів", $"""
            <table><tbody>
            <tr><th>Всього клієнтів</th><td>{data.CustomerStats.TotalCustomers}</td></tr>
            <tr><th>Нових за 30 днів</th><td>{data.CustomerStats.NewCustomersLast30Days}</td></tr>
            <tr><th>Середній чек</th><td>{data.CustomerStats.AverageOrderValue:N2} грн</td></tr>
            </tbody></table>
            """);

    private static string BuildTable(string[] headers, IEnumerable<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<table><thead><tr>");
        foreach (var h in headers) sb.Append($"<th>{h}</th>");
        sb.Append("</tr></thead><tbody>");
        foreach (var row in rows)
        {
            sb.Append("<tr>");
            foreach (var cell in row) sb.Append($"<td>{cell}</td>");
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string WrapReport(string title, string body)
    {
        var now = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        return $@"<!DOCTYPE html>
<html lang=""uk""><head><meta charset=""utf-8""/><title>{title}</title>
<style>
body{{font-family:Segoe UI,sans-serif;padding:32px;color:#172119}}
h1{{color:#2d5a3f}} table{{width:100%;border-collapse:collapse;margin-top:16px}}
th,td{{border:1px solid #ccc;padding:10px;text-align:left}}
th{{background:#e8f5f0}}
.meta{{color:#555;font-size:14px}}
@media print {{ .no-print {{ display:none }} }}
</style></head>
<body>
<p class=""meta"">Garden Tools · {now}</p>
<h1>{title}</h1>
{body}
<p class=""no-print"" style=""margin-top:24px"">
<button onclick=""window.print()"">Друк / зберегти як PDF</button>
</p>
</body></html>";
    }
}
