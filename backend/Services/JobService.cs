using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public sealed class JobService(
    AppDbContext dbContext,
    IJobFetcherService jobFetcher,
    IAiExtractorService aiExtractor,
    TimeProvider timeProvider,
    ILogger<JobService> logger) : IJobService
{
    public async Task<IReadOnlyList<JobPostingResponse>> GetJobsAsync(
        string? status,
        string? source,
        string? search,
        CancellationToken cancellationToken)
    {
        var statusFilter = ParseFilter<JobStatus>(status, "status");
        var sourceFilter = ParseFilter<JobSource>(source, "source");
        var query = dbContext.JobPostings.AsNoTracking();

        if (statusFilter is not null)
        {
            query = query.Where(job => job.Status == statusFilter);
        }

        if (sourceFilter is not null)
        {
            query = query.Where(job => job.Source == sourceFilter);
        }

        var jobs = await query
            .OrderByDescending(job => job.PublishedAt)
            .ToListAsync(cancellationToken);

        var normalizedSearch = search?.Trim();
        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            jobs = jobs
                .Where(job =>
                    job.CompanyName.Contains(
                        normalizedSearch,
                        StringComparison.OrdinalIgnoreCase) ||
                    job.JobTitle.Contains(
                        normalizedSearch,
                        StringComparison.OrdinalIgnoreCase) ||
                    (job.Location?.Contains(
                        normalizedSearch,
                        StringComparison.OrdinalIgnoreCase) ?? false) ||
                    job.Qualifications.Any(qualification =>
                        qualification.Contains(
                            normalizedSearch,
                            StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return jobs.Select(ToResponse).ToArray();
    }

    public async Task<RefreshResponse> RefreshAsync(
        CancellationToken cancellationToken)
    {
        var fetchResult = await jobFetcher.FetchAsync(cancellationToken);
        var existingUrls = await dbContext.JobPostings
            .AsNoTracking()
            .Select(job => job.ExternalUrl)
            .ToListAsync(cancellationToken);
        var knownUrls = new HashSet<string>(
            existingUrls,
            StringComparer.OrdinalIgnoreCase);
        var relevantArticles = fetchResult.Articles
            .Where(article => knownUrls.Add(article.ExternalUrl))
            .Where(article =>
                ItJobFilter.IsRelevant(article.Title, article.CleanContent))
            .ToArray();

        logger.LogInformation(
            "Fetched {ArticleCount} articles, {RelevantCount} new IT postings, and {FailedSourceCount} failed sources",
            fetchResult.Articles.Count,
            relevantArticles.Length,
            fetchResult.FailedSources.Count);

        var newJobs = new List<JobPosting>(relevantArticles.Length);
        foreach (var article in relevantArticles)
        {
            var extraction = await aiExtractor.ExtractAsync(
                article,
                cancellationToken);

            newJobs.Add(new JobPosting
            {
                ExternalUrl = article.ExternalUrl,
                Source = article.Source,
                CompanyName = extraction.CompanyName,
                JobTitle = extraction.JobTitle,
                Qualifications = extraction.Qualifications.ToArray(),
                Location = extraction.Location,
                Status = JobStatus.NEW,
                PublishedAt = article.PublishedAt,
                CreatedAt = timeProvider.GetUtcNow()
            });
        }

        if (newJobs.Count > 0)
        {
            dbContext.JobPostings.AddRange(newJobs);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var message = newJobs.Count == 0
            ? "Tidak ada lowongan IT baru sejak refresh terakhir."
            : $"Ditemukan {newJobs.Count} lowongan IT baru.";

        if (fetchResult.FailedSources.Count > 0)
        {
            message +=
                $" Sebagian sumber gagal diperiksa: {string.Join(", ", fetchResult.FailedSources)}.";
        }

        return new RefreshResponse(
            newJobs.Count,
            message,
            timeProvider.GetUtcNow());
    }

    public async Task<JobPostingResponse> UpdateStatusAsync(
        int id,
        JobStatus status,
        CancellationToken cancellationToken)
    {
        var job = await dbContext.JobPostings
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (job is null)
        {
            throw new KeyNotFoundException(
                $"Job posting with ID {id} was not found.");
        }

        job.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(job);
    }

    private static JobPostingResponse ToResponse(JobPosting job)
    {
        return new JobPostingResponse(
            job.Id,
            job.ExternalUrl,
            job.Source,
            job.CompanyName,
            job.JobTitle,
            job.Qualifications,
            job.Location,
            job.Status,
            job.PublishedAt,
            job.CreatedAt);
    }

    private static TEnum? ParseFilter<TEnum>(
        string? value,
        string parameterName)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var validNames = Enum.GetNames<TEnum>();
        if (!validNames.Contains(value, StringComparer.OrdinalIgnoreCase) ||
            !Enum.TryParse<TEnum>(value, true, out var parsed))
        {
            throw new ArgumentException(
                $"The {parameterName} filter is invalid.",
                parameterName);
        }

        return parsed;
    }
}
