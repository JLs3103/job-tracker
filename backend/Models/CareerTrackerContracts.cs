using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public sealed record JobApplicationResponse(
    int Id,
    string Company,
    string JobTitle,
    string AppliedVia,
    string? AppliedLink,
    DateTimeOffset AppliedDate,
    DateTimeOffset LastUpdated,
    string Status,
    ApplicationResult Result,
    int? SourceJobPostingId);

public sealed record CreateJobApplicationRequest
{
    [Required, MaxLength(300)]
    public required string Company { get; init; }

    [Required, MaxLength(300)]
    public required string JobTitle { get; init; }

    [MaxLength(120)]
    public string AppliedVia { get; init; } = "Career Site";

    [MaxLength(2048)]
    public string? AppliedLink { get; init; }

    public DateTimeOffset? AppliedDate { get; init; }

    public DateTimeOffset? LastUpdated { get; init; }

    [MaxLength(120)]
    public string Status { get; init; } = "Applied";

    public ApplicationResult Result { get; init; } = ApplicationResult.InProgress;
}

public sealed record UpdateJobApplicationRequest
{
    [MaxLength(300)]
    public string? Company { get; init; }

    [MaxLength(300)]
    public string? JobTitle { get; init; }

    [MaxLength(120)]
    public string? AppliedVia { get; init; }

    [MaxLength(2048)]
    public string? AppliedLink { get; init; }

    public DateTimeOffset? AppliedDate { get; init; }

    public DateTimeOffset? LastUpdated { get; init; }

    [MaxLength(120)]
    public string? Status { get; init; }

    public ApplicationResult? Result { get; init; }
}

public sealed record OnGoingJobResponse(
    int Id,
    string Company,
    string JobTitle,
    string Link,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record CreateOnGoingJobRequest
{
    [Required, MaxLength(300)]
    public required string Company { get; init; }

    [Required, MaxLength(300)]
    public required string JobTitle { get; init; }

    [Required, MaxLength(2048)]
    public required string Link { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}

public sealed record UpdateOnGoingJobRequest
{
    [MaxLength(300)]
    public string? Company { get; init; }

    [MaxLength(300)]
    public string? JobTitle { get; init; }

    [MaxLength(2048)]
    public string? Link { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}

public sealed record MoveOnGoingToAppliedRequest
{
    [MaxLength(120)]
    public string? AppliedVia { get; init; }
}

public sealed record CareerSiteResponse(
    int Id,
    string Name,
    string Url,
    string? LastJobUpdated,
    DateTimeOffset? LastCheckedAt);

public sealed record CreateCareerSiteRequest
{
    [Required, MaxLength(300)]
    public required string Name { get; init; }

    [Required, MaxLength(2048)]
    public required string Url { get; init; }

    [MaxLength(500)]
    public string? LastJobUpdated { get; init; }
}

public sealed record UpdateCareerSiteRequest
{
    [MaxLength(300)]
    public string? Name { get; init; }

    [MaxLength(2048)]
    public string? Url { get; init; }

    [MaxLength(500)]
    public string? LastJobUpdated { get; init; }
}
