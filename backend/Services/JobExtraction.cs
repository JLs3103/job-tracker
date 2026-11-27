namespace backend.Services;

/// <summary>Contains the short summary fields extracted from a job article.</summary>
public sealed record JobExtraction(
    string CompanyName,
    string JobTitle,
    IReadOnlyList<string> Qualifications,
    string? Location,
    bool IsItJob = true,
    int? MatchScore = null,
    string? MatchReason = null,
    IReadOnlyList<string>? TargetRoles = null,
    IReadOnlyList<string>? TechnicalSkills = null);

public static class JobExtractionFallback
{
    public const string CompanyName = "Belum teridentifikasi";
    public const string Qualification =
        "Ringkasan otomatis belum tersedia. Buka sumber lowongan untuk memeriksa rincian.";

    public static bool IsFallback(
        string companyName,
        IReadOnlyList<string> qualifications)
    {
        return companyName.Equals(CompanyName, StringComparison.Ordinal) &&
            qualifications.Contains(Qualification, StringComparer.Ordinal);
    }
}
