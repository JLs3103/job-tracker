using backend.Models;

namespace backend.Services;

/// <summary>Contains normalized text fetched from a public job source.</summary>
public sealed record JobArticle(
    JobSource Source,
    string Title,
    string CleanContent,
    string ExternalUrl,
    DateTimeOffset PublishedAt);

/// <summary>Contains fetched articles and any sources that could not be read.</summary>
public sealed record JobFetchResult(
    IReadOnlyList<JobArticle> Articles,
    IReadOnlyList<string> FailedSources);
