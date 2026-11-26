using System.Text.RegularExpressions;

namespace backend.Services;

public static partial class ItJobFilter
{
    [GeneratedRegex(
        @"(?<![\p{L}\p{N}])(?:IT|Developer|Frontend|Backend|Full\s*Stack|Fullstack|Programmer|Software|Web|React|Node(?:\.js)?|\.NET|Cyber|Security|SOC|Blue\s+Team|Network|Sysadmin|DevOps|QA|UI\s*/\s*UX|Data|Cloud)(?![\p{L}\p{N}])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ItKeywordPattern();

    public static bool IsRelevant(string title, string cleanContent)
    {
        return ItKeywordPattern().IsMatch($"{title}\n{cleanContent}");
    }
}
