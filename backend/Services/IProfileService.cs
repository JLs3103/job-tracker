using backend.Models;

namespace backend.Services;

public interface IProfileService
{
    Task<CandidateProfile> GetProfileAsync(CancellationToken cancellationToken);

    Task<CandidateProfile> UpdateProfileAsync(
        CandidateProfile profile,
        CancellationToken cancellationToken);
}
