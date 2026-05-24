using System.Globalization;

namespace GardenTolls.Web.Infrastructure;

public static class KyivTime
{
    private static readonly TimeZoneInfo Zone = ResolveKyivZone();

    private static TimeZoneInfo ResolveKyivZone()
    {
        foreach (var id in new[] { "Europe/Kyiv", "Europe/Kiev", "FLE Standard Time", "E. Europe Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("KyivFallback", TimeSpan.FromHours(2), "Kyiv", "Kyiv");
    }

    public static DateTime ToKyiv(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
        return TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);
    }

    public static string FormatReviewDateTime(DateTime reviewDateUtc)
    {
        var kyiv = ToKyiv(reviewDateUtc);
        var culture = CultureInfo.GetCultureInfo("uk-UA");
        return kyiv.ToString("d MMMM yyyy 'р.' о HH:mm:ss", culture) + " (за київським часом)";
    }
}
