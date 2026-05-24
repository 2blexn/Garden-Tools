using System.Text.RegularExpressions;

namespace GardenTolls.Web.Services;

public interface IContentModerationService
{
    ModerationResult Analyze(string? text);
}

public class ModerationResult
{
    public int RiskScore { get; set; }
    public bool IsHighRisk => RiskScore >= 70;
    public bool IsMediumRisk => RiskScore >= 40 && RiskScore < 70;
    public List<string> Flags { get; set; } = [];
    public string Summary => Flags.Count == 0 ? "Порушень не виявлено" : string.Join(" · ", Flags);
}

public partial class ContentModerationService : IContentModerationService
{
    private static readonly string[] Profanity =
    [
        "бля", "хуй", "пізд", "їбат", "сука", "мудак", "debil", "fuck", "shit", "asshole"
    ];

    /// <summary>Лише ці тригери дають прапорець «Пропаганда війни / політичний контент».</summary>
    private static readonly string[] WarPropagandaTriggers =
    [
        "хохл", "хахл", "zoro", "крим наш", "бамбят",
        "негр", "індус", "жид", "чурк", "хач", "циган"
    ];

    private static readonly string[] SpamPatterns =
    [
        @"https?://", @"www\.", @"\.ru\b", @"\.su\b", @"telegram\.me", @"t\.me/"
    ];

    public ModerationResult Analyze(string? text)
    {
        var result = new ModerationResult();
        if (string.IsNullOrWhiteSpace(text))
            return result;

        var normalized = text.ToLowerInvariant();

        if (Profanity.Any(w => normalized.Contains(w, StringComparison.Ordinal)))
        {
            result.Flags.Add("Нецензурна лексика");
            result.RiskScore += 45;
        }

        if (ContainsWarPropaganda(normalized))
        {
            result.Flags.Add("Пропаганда війни / політичний контент");
            result.RiskScore += 55;
        }

        foreach (var pattern in SpamPatterns)
        {
            if (Regex.IsMatch(normalized, pattern, RegexOptions.IgnoreCase))
            {
                result.Flags.Add("Підозрілі посилання / спам");
                result.RiskScore += 25;
                break;
            }
        }

        if (text.Length > 800)
        {
            result.Flags.Add("Занадто довгий текст");
            result.RiskScore += 15;
        }

        if (Regex.IsMatch(text, @"(.)\1{6,}"))
        {
            result.Flags.Add("Повторювані символи");
            result.RiskScore += 10;
        }

        result.RiskScore = Math.Min(100, result.RiskScore);
        return result;
    }

    private static bool ContainsWarPropaganda(string normalized)
    {
        foreach (var trigger in WarPropagandaTriggers)
        {
            if (normalized.Contains(trigger, StringComparison.Ordinal))
                return true;
        }

        if (Regex.IsMatch(normalized, @"(?<![a-zа-яіїєґ])z(?![a-zа-яіїєґ])", RegexOptions.IgnoreCase))
            return true;

        return false;
    }
}
