namespace backend.Services;

public interface IJobFetcherService
{
    Task<JobFetchResult> FetchAsync(CancellationToken cancellationToken);
}
