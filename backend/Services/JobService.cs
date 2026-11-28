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

    public async Task<RefreshStateResponse> GetRefreshStateAsync(
        CancellationToken cancellationToken)
    {
        var refreshState = await dbContext.JobRefreshStates
            .AsNoTracking()
            .SingleOrDefaultAsync(
                state => state.Id == JobRefreshState.SingletonId,
                cancellationToken);

        return new RefreshStateResponse(refreshState?.LastSuccessfulRefreshAt);
    }

    public async Task<RefreshResponse> RefreshAsync(
        CancellationToken cancellationToken)
    {
        var refreshedAt = timeProvider.GetUtcNow();
        var refreshState = await dbContext.JobRefreshStates
            .SingleOrDefaultAsync(
                state => state.Id == JobRefreshState.SingletonId,
                cancellationToken);
        var sevenDaysAgo = refreshedAt.AddDays(-7);
        var publishedSince = refreshState is null ||
            refreshState.LastSuccessfulRefreshAt < sevenDaysAgo
                ? sevenDaysAgo
                : refreshState.LastSuccessfulRefreshAt;
        if (publishedSince > refreshedAt)
        {
            publishedSince = refreshedAt;
        }

        logger.LogInformation(
            "Checking job postings published since {PublishedSince}; last successful refresh was {LastSuccessfulRefreshAt}",
            publishedSince,
            refreshState?.LastSuccessfulRefreshAt);

        var fetchResult = await jobFetcher.FetchAsync(
            cancellationToken,
            publishedSince);
        var existingUrls = await dbContext.JobPostings
            .AsNoTracking()
            .Select(job => job.ExternalUrl)
            .ToListAsync(cancellationToken);
        var knownUrls = new HashSet<string>(
            existingUrls,
            StringComparer.OrdinalIgnoreCase);
        var relevantArticles = fetchResult.Articles
            .Where(article => knownUrls.Add(article.ExternalUrl))
            .Where(article => article.PublishedAt <= refreshedAt)
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
            if (!extraction.IsItJob)
            {
                logger.LogInformation(
                    "Skipping non-IT job posting after AI classification for {JobUrl}",
                    article.ExternalUrl);
                continue;
            }

            var newJob = new JobPosting
            {
                ExternalUrl = article.ExternalUrl,
                Source = article.Source,
                CompanyName = extraction.CompanyName,
                JobTitle = extraction.JobTitle,
                Qualifications = extraction.Qualifications.ToArray(),
                TargetRoles = extraction.TargetRoles?.ToArray() ?? [],
                TechnicalSkills = extraction.TechnicalSkills?.ToArray() ?? [],
                Location = extraction.Location,
                MatchScore = extraction.MatchScore,
                MatchReason = extraction.MatchReason,
                Status = JobStatus.NEW,
                PublishedAt = article.PublishedAt,
                CreatedAt = timeProvider.GetUtcNow()
            };
            dbContext.JobPostings.Add(newJob);
            await dbContext.SaveChangesAsync(cancellationToken);
            newJobs.Add(newJob);
        }

        if (fetchResult.FailedSources.Count == 0)
        {
            if (refreshState is null)
            {
                dbContext.JobRefreshStates.Add(new JobRefreshState
                {
                    LastSuccessfulRefreshAt = refreshedAt
                });
            }
            else
            {
                refreshState.LastSuccessfulRefreshAt = refreshedAt;
            }

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

    public async Task<JobPostingResponse> RetryExtractionAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var job = await dbContext.JobPostings
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (job is null)
        {
            throw new KeyNotFoundException(
                $"Job posting with ID {id} was not found.");
        }

        if (!JobExtractionFallback.IsFallback(
                job.CompanyName,
                job.Qualifications))
        {
            return ToResponse(job);
        }

        var fetchResult = await jobFetcher.FetchAsync(
            cancellationToken,
            job.PublishedAt,
            job.Source);
        if (fetchResult.FailedSources.Contains(
                job.Source.ToString(),
                StringComparer.Ordinal))
        {
            throw new JobSourcesUnavailableException(
                $"Unable to fetch job postings from {job.Source}.");
        }

        var article = fetchResult.Articles.FirstOrDefault(item =>
            item.ExternalUrl.Equals(
                job.ExternalUrl,
                StringComparison.OrdinalIgnoreCase));
        if (article is null)
        {
            throw new JobArticleUnavailableException();
        }

        var extraction = await aiExtractor.ExtractAsync(
            article,
            cancellationToken);
        if (!extraction.IsItJob)
        {
            logger.LogInformation(
                "AI classification marked existing job {JobId} as non-IT; keeping existing record unchanged.",
                job.Id);
            return ToResponse(job);
        }

        if (JobExtractionFallback.IsFallback(
                extraction.CompanyName,
                extraction.Qualifications))
        {
            throw new AiExtractionUnavailableException();
        }

        job.CompanyName = extraction.CompanyName;
        job.JobTitle = extraction.JobTitle;
        job.Qualifications = extraction.Qualifications.ToArray();
        job.TargetRoles = extraction.TargetRoles?.ToArray() ?? [];
        job.TechnicalSkills = extraction.TechnicalSkills?.ToArray() ?? [];
        job.Location = extraction.Location;
        job.MatchScore = extraction.MatchScore;
        job.MatchReason = extraction.MatchReason;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(job);
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

        if (status == JobStatus.APPLIED)
        {
            var alreadyTracked = await dbContext.JobApplications.AnyAsync(
                item => item.SourceJobPostingId == job.Id ||
                    (item.AppliedLink != null && item.AppliedLink == job.ExternalUrl),
                cancellationToken);

            if (!alreadyTracked)
            {
                var now = timeProvider.GetUtcNow();
                dbContext.JobApplications.Add(new JobApplication
                {
                    Company = job.CompanyName,
                    JobTitle = job.JobTitle,
                    AppliedVia = $"Lowongan IT ({job.Source})",
                    AppliedLink = job.ExternalUrl,
                    AppliedDate = now,
                    LastUpdated = now,
                    Status = "Applied",
                    Result = ApplicationResult.InProgress,
                    SourceJobPostingId = job.Id
                });
            }
        }

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
            job.TargetRoles,
            job.TechnicalSkills,
            job.Location,
            job.MatchScore,
            job.MatchReason,
            job.Status,
            job.PublishedAt,
            job.CreatedAt,
            JobExtractionFallback.IsFallback(
                job.CompanyName,
                job.Qualifications));
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
