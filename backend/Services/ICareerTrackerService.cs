using backend.Models;

namespace backend.Services;

public interface ICareerTrackerService
{
    Task<IReadOnlyList<JobApplicationResponse>> GetApplicationsAsync(
        ApplicationResult? result,
        string? search,
        CancellationToken cancellationToken);

    Task<JobApplicationResponse> CreateApplicationAsync(
        CreateJobApplicationRequest request,
        CancellationToken cancellationToken);

    Task<JobApplicationResponse> UpdateApplicationAsync(
        int id,
        UpdateJobApplicationRequest request,
        CancellationToken cancellationToken);

    Task DeleteApplicationAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<OnGoingJobResponse>> GetOnGoingAsync(
        string? search,
        CancellationToken cancellationToken);

    Task<OnGoingJobResponse> CreateOnGoingAsync(
        CreateOnGoingJobRequest request,
        CancellationToken cancellationToken);

    Task<OnGoingJobResponse> UpdateOnGoingAsync(
        int id,
        UpdateOnGoingJobRequest request,
        CancellationToken cancellationToken);

    Task<JobApplicationResponse> MoveOnGoingToAppliedAsync(
        int id,
        string? appliedVia,
        CancellationToken cancellationToken);

    Task DeleteOnGoingAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CareerSiteResponse>> GetCareerSitesAsync(
        CancellationToken cancellationToken);

    Task<CareerSiteResponse> CreateCareerSiteAsync(
        CreateCareerSiteRequest request,
        CancellationToken cancellationToken);

    Task<CareerSiteResponse> UpdateCareerSiteAsync(
        int id,
        UpdateCareerSiteRequest request,
        CancellationToken cancellationToken);

    Task DeleteCareerSiteAsync(int id, CancellationToken cancellationToken);
}
