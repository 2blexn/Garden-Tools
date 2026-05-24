namespace GardenTolls.Web.Infrastructure;

public static class SeasonFilter
{
    public static readonly IReadOnlyDictionary<string, string[]> Keywords = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["spring"] = ["весн", "spring", "посад", "насін"],
        ["summer"] = ["літ", "summer", "полив", "спека"],
        ["autumn"] = ["осін", "autumn", "fall", "збир"],
        ["winter"] = ["зим", "winter", "сніг", "захист"]
    };

    public static IReadOnlyList<(string Value, string Label)> Options { get; } =
    [
        ("spring", "Весна"),
        ("summer", "Літо"),
        ("autumn", "Осінь"),
        ("winter", "Зима")
    ];
}
