using backend.Models;

namespace backend.Services;

public interface IJobFetcherService
{
    Task<JobFetchResult> FetchAsync(
        CancellationToken cancellationToken,
        DateTimeOffset? publishedSince = null,
        JobSource? source = null);
}
