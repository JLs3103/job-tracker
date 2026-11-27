using backend.Models;

namespace backend.Services;

public interface IAiExtractorService
{
    Task<JobExtraction> ExtractAsync(
        JobArticle article,
        CancellationToken cancellationToken);

    Task<CandidateProfile> ExtractProfileFromCvAsync(
        string cvText,
        CancellationToken cancellationToken);
}
