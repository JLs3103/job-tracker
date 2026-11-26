namespace backend.Models;

/// <summary>Summarizes the outcome of checking job sources.</summary>
public sealed record RefreshResponse(
    int NewJobsCount,
    string Message,
    DateTimeOffset CheckedAt);
