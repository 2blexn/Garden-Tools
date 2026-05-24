using GardenTolls.Web.Data;
using GardenTolls.Web.Models;
using GardenTolls.Web.ViewModels.Home;
using Microsoft.EntityFrameworkCore;

namespace GardenTolls.Web.Services;

public interface ISearchService
{
    Task LogSearchAsync(string? query, int? userId);
    Task<SearchSuggestionsViewModel> GetSuggestionsAsync(int? userId, string? query = null);
}

public class SearchService : ISearchService
{
    private static readonly string[] Templates =
    [
        "насіння", "лопата", "полив", "секатор", "добрива", "теплиця",
        "снігоприбирач", "розсада", "горщик", "шланг", "граблі", "садовий інструмент"
    ];

    private readonly AppDbContext _db;

    public SearchService(AppDbContext db) => _db = db;

    public async Task LogSearchAsync(string? query, int? userId)
    {
        var q = query?.Trim();
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2) return;

        _db.SearchQueryLogs.Add(new SearchQueryLog
        {
            UserId = userId,
            Query = q.Length > 200 ? q[..200] : q,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task<SearchSuggestionsViewModel> GetSuggestionsAsync(int? userId, string? query = null)
    {
        var vm = new SearchSuggestionsViewModel();
        var q = query?.Trim();

        vm.PopularQueries = await _db.SearchQueryLogs
            .Where(l => l.Query.Length >= 2)
            .GroupBy(l => l.Query.ToLower())
            .Select(g => new { Query = g.First().Query, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(2)
            .Select(x => x.Query)
            .ToListAsync();

        if (userId.HasValue)
        {
            vm.UserHistory = await _db.SearchQueryLogs
                .Where(l => l.UserId == userId.Value)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => l.Query)
                .Take(15)
                .ToListAsync();

            vm.UserHistory = vm.UserHistory
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var hasCatalogMatch = await HasCatalogMatchAsync(q);
            if (hasCatalogMatch)
            {
                var suggestions = Templates
                    .Where(t => t.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                suggestions.AddRange(await GetCatalogSuggestionTermsAsync(q, 6));
                vm.Templates = suggestions
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(8)
                    .ToList();
            }
        }

        return vm;
    }

    private async Task<bool> HasCatalogMatchAsync(string q)
    {
        if (await _db.Categories.AnyAsync(c =>
                c.IsActive && (c.Name.Contains(q) || (c.Description != null && c.Description.Contains(q)))))
            return true;

        if (await _db.Products.AnyAsync(p =>
                !p.IsDiscontinued &&
                (p.ProductName.Contains(q) ||
                 p.SKU.Contains(q) ||
                 (p.Description != null && p.Description.Contains(q)))))
            return true;

        if (await _db.Promotions.AnyAsync(p =>
                p.IsActive &&
                (p.PromotionName.Contains(q) ||
                 (p.Description != null && p.Description.Contains(q)))))
            return true;

        return false;
    }

    private async Task<List<string>> GetCatalogSuggestionTermsAsync(string q, int limit)
    {
        var terms = new List<string>();

        var categories = await _db.Categories
            .Where(c => c.IsActive && c.Name.Contains(q))
            .Select(c => c.Name)
            .Take(limit)
            .ToListAsync();
        terms.AddRange(categories);

        var products = await _db.Products
            .Where(p => !p.IsDiscontinued && p.ProductName.Contains(q))
            .Select(p => p.ProductName)
            .Take(limit)
            .ToListAsync();
        terms.AddRange(products);

        var promos = await _db.Promotions
            .Where(p => p.IsActive && p.PromotionName.Contains(q))
            .Select(p => p.PromotionName)
            .Take(limit)
            .ToListAsync();
        terms.AddRange(promos);

        return terms
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }
}
