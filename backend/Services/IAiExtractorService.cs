namespace backend.Services;

public interface IAiExtractorService
{
    Task<JobExtraction> ExtractAsync(
        JobArticle article,
        CancellationToken cancellationToken);
}
