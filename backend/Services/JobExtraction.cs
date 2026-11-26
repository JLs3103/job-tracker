namespace backend.Services;

/// <summary>Contains the short summary fields extracted from a job article.</summary>
public sealed record JobExtraction(
    string CompanyName,
    string JobTitle,
    IReadOnlyList<string> Qualifications,
    string? Location);
