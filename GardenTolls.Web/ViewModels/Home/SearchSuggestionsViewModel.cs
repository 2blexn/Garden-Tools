namespace GardenTolls.Web.ViewModels.Home;

public class SearchSuggestionsViewModel
{
    public List<string> Templates { get; set; } = [];
    public List<string> UserHistory { get; set; } = [];
    public List<string> PopularQueries { get; set; } = [];
}
