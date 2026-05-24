using GardenTolls.Web.Infrastructure;
using GardenTolls.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GardenTolls.Web.ViewComponents;

public class SearchSuggestionsViewComponent : ViewComponent
{
    private readonly ISearchService _search;

    public SearchSuggestionsViewComponent(ISearchService search) => _search = search;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = HttpContext.User.GetUserId();
        var vm = await _search.GetSuggestionsAsync(userId, query: null);
        return View(vm);
    }
}
