using backend.Models;

namespace backend.Services;

public interface IJobService
{
    Task<IReadOnlyList<JobPostingResponse>> GetJobsAsync(
        string? status,
        string? source,
        string? search,
        CancellationToken cancellationToken);

    Task<RefreshResponse> RefreshAsync(CancellationToken cancellationToken);

    Task<JobPostingResponse> UpdateStatusAsync(
        int id,
        JobStatus status,
        CancellationToken cancellationToken);
}
