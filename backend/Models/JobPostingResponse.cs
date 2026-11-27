namespace backend.Models;

/// <summary>Represents a job posting returned by the API.</summary>
public sealed record JobPostingResponse(
    int Id,
    string ExternalUrl,
    JobSource Source,
    string CompanyName,
    string JobTitle,
    IReadOnlyList<string> Qualifications,
    string? Location,
    JobStatus Status,
    DateTimeOffset PublishedAt,
    DateTimeOffset CreatedAt,
    bool CanRetryExtraction);
