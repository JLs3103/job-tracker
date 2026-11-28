using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public sealed class CareerTrackerService(
    AppDbContext dbContext,
    TimeProvider timeProvider) : ICareerTrackerService
{
    public async Task<IReadOnlyList<JobApplicationResponse>> GetApplicationsAsync(
        ApplicationResult? result,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = dbContext.JobApplications.AsNoTracking();
        if (result is not null)
        {
            query = query.Where(item => item.Result == result.Value);
        }

        var items = await query
            .OrderByDescending(item => item.AppliedDate)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);

        var normalizedSearch = search?.Trim();
        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            items = items
                .Where(item =>
                    item.Company.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    item.JobTitle.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    item.AppliedVia.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    item.Status.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return items.Select(ToResponse).ToArray();
    }

    public async Task<JobApplicationResponse> CreateApplicationAsync(
        CreateJobApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var appliedDate = request.AppliedDate ?? timeProvider.GetUtcNow();
        var lastUpdated = request.LastUpdated ?? appliedDate;

        var entity = new JobApplication
        {
            Company = request.Company.Trim(),
            JobTitle = request.JobTitle.Trim(),
            AppliedVia = string.IsNullOrWhiteSpace(request.AppliedVia)
                ? "Career Site"
                : request.AppliedVia.Trim(),
            AppliedLink = NormalizeOptional(request.AppliedLink),
            AppliedDate = appliedDate,
            LastUpdated = lastUpdated,
            Status = string.IsNullOrWhiteSpace(request.Status)
                ? "Applied"
                : request.Status.Trim(),
            Result = request.Result
        };

        dbContext.JobApplications.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<JobApplicationResponse> UpdateApplicationAsync(
        int id,
        UpdateJobApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.JobApplications
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Job application with ID {id} was not found.");

        if (!string.IsNullOrWhiteSpace(request.Company))
            entity.Company = request.Company.Trim();
        if (!string.IsNullOrWhiteSpace(request.JobTitle))
            entity.JobTitle = request.JobTitle.Trim();
        if (!string.IsNullOrWhiteSpace(request.AppliedVia))
            entity.AppliedVia = request.AppliedVia.Trim();
        if (request.AppliedLink is not null)
            entity.AppliedLink = NormalizeOptional(request.AppliedLink);
        if (request.AppliedDate is not null)
            entity.AppliedDate = request.AppliedDate.Value;

        var changedProgress = false;
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            !entity.Status.Equals(request.Status.Trim(), StringComparison.Ordinal))
        {
            entity.Status = request.Status.Trim();
            changedProgress = true;
        }

        if (request.Result is not null && entity.Result != request.Result.Value)
        {
            entity.Result = request.Result.Value;
            changedProgress = true;
        }

        if (request.LastUpdated is not null)
        {
            entity.LastUpdated = request.LastUpdated.Value;
        }
        else if (changedProgress)
        {
            entity.LastUpdated = timeProvider.GetUtcNow();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task DeleteApplicationAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.JobApplications
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Job application with ID {id} was not found.");

        dbContext.JobApplications.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OnGoingJobResponse>> GetOnGoingAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.OnGoingJobs
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);

        var normalizedSearch = search?.Trim();
        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            items = items
                .Where(item =>
                    item.Company.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    item.JobTitle.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    (item.Notes?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        return items.Select(ToResponse).ToArray();
    }

    public async Task<OnGoingJobResponse> CreateOnGoingAsync(
        CreateOnGoingJobRequest request,
        CancellationToken cancellationToken)
    {
        var link = request.Link.Trim();
        var entity = new OnGoingJob
        {
            Company = request.Company.Trim(),
            JobTitle = request.JobTitle.Trim(),
            Link = link,
            Notes = NormalizeOptional(request.Notes) ?? InferSiteName(link),
            CreatedAt = timeProvider.GetUtcNow()
        };

        dbContext.OnGoingJobs.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<OnGoingJobResponse> UpdateOnGoingAsync(
        int id,
        UpdateOnGoingJobRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.OnGoingJobs
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"On-going job with ID {id} was not found.");

        if (!string.IsNullOrWhiteSpace(request.Company))
            entity.Company = request.Company.Trim();
        if (!string.IsNullOrWhiteSpace(request.JobTitle))
            entity.JobTitle = request.JobTitle.Trim();
        if (!string.IsNullOrWhiteSpace(request.Link))
            entity.Link = request.Link.Trim();
        if (request.Notes is not null)
            entity.Notes = NormalizeOptional(request.Notes);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<JobApplicationResponse> MoveOnGoingToAppliedAsync(
        int id,
        string? appliedVia,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.OnGoingJobs
            .FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"On-going job with ID {id} was not found.");

        var now = timeProvider.GetUtcNow();
        var application = new JobApplication
        {
            Company = item.Company,
            JobTitle = item.JobTitle,
            AppliedVia = string.IsNullOrWhiteSpace(appliedVia)
                ? InferAppliedVia(item.Link, item.Notes)
                : appliedVia.Trim(),
            AppliedLink = item.Link,
            AppliedDate = now,
            LastUpdated = now,
            Status = "Applied",
            Result = ApplicationResult.InProgress
        };

        dbContext.JobApplications.Add(application);
        dbContext.OnGoingJobs.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(application);
    }

    public async Task DeleteOnGoingAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.OnGoingJobs
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"On-going job with ID {id} was not found.");

        dbContext.OnGoingJobs.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CareerSiteResponse>> GetCareerSitesAsync(
        CancellationToken cancellationToken)
    {
        var items = await dbContext.CareerSites
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);

        return items.Select(ToResponse).ToArray();
    }

    public async Task<CareerSiteResponse> CreateCareerSiteAsync(
        CreateCareerSiteRequest request,
        CancellationToken cancellationToken)
    {
        var lastJobUpdated = NormalizeOptional(request.LastJobUpdated);
        var entity = new CareerSite
        {
            Name = request.Name.Trim(),
            Url = request.Url.Trim(),
            LastJobUpdated = lastJobUpdated,
            LastCheckedAt = lastJobUpdated is not null ? timeProvider.GetUtcNow() : null
        };

        dbContext.CareerSites.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task<CareerSiteResponse> UpdateCareerSiteAsync(
        int id,
        UpdateCareerSiteRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.CareerSites
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Career site with ID {id} was not found.");

        if (!string.IsNullOrWhiteSpace(request.Name))
            entity.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Url))
            entity.Url = request.Url.Trim();
        if (request.LastJobUpdated is not null)
        {
            entity.LastJobUpdated = NormalizeOptional(request.LastJobUpdated);
            entity.LastCheckedAt = timeProvider.GetUtcNow();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(entity);
    }

    public async Task DeleteCareerSiteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.CareerSites
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Career site with ID {id} was not found.");

        dbContext.CareerSites.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string InferAppliedVia(string link, string? notes)
    {
        if (!string.IsNullOrWhiteSpace(notes))
            return notes.Trim();
        return InferSiteName(link) ?? "Career Site";
    }

    private static string? InferSiteName(string link)
    {
        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            return null;
        }

        var host = uri.Host.ToLowerInvariant();
        if (host.Contains("disnakerja.com")) return "Disnakerja";
        if (host.Contains("bukajobs.com")) return "Bukajobs";
        if (host.Contains("browseloker.")) return "BrowseLoker";
        if (host.Contains("lokerblog.com")) return "Loker Blog";
        if (host.Contains("lokerbumn.com")) return "LokerBUMN";
        if (host.Contains("bersamabumn.com")) return "BersamaBUMN";
        if (host.Contains("openkerja.id")) return "OpenKerja";
        if (host.Contains("okejobs.com")) return "OkeJobs";
        if (host.Contains("instagram.com")) return "Instagram";
        if (host.Contains("linkedin.com") || host.Contains("lnkd.in")) return "LinkedIn";
        if (host.Contains("indeed.com")) return "Indeed";
        if (host.Contains("glints.com")) return "Glints";
        if (host.Contains("kalibrr.com")) return "Kalibrr";
        if (host.Contains("dealls.com")) return "Dealls";
        if (host.Contains("jobstreet.")) return "JobStreet";
        if (host.Contains("glassdoor.")) return "Glassdoor";
        if (host.Contains("pintarnya.com")) return "Pintarnya";
        if (host.Contains("cake.me") || host.Contains("cakeresume.com")) return "Cake";
        if (host.Contains("techinasia.com")) return "Tech in Asia";

        return FormatHostBrand(host);
    }

    private static string FormatHostBrand(string host)
    {
        var parts = host
            .Split('.', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        string[] prefixes = ["www", "karir", "career", "careers", "jobs", "job", "rekrutmen", "recruitment", "join", "m"];
        while (parts.Count > 2 && prefixes.Contains(parts[0]))
        {
            parts.RemoveAt(0);
        }

        var brand = parts[0];
        if (brand.Length <= 3)
        {
            return brand.ToUpperInvariant();
        }

        return char.ToUpperInvariant(brand[0]) + brand[1..];
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static JobApplicationResponse ToResponse(JobApplication item) => new(
        item.Id,
        item.Company,
        item.JobTitle,
        item.AppliedVia,
        item.AppliedLink,
        item.AppliedDate,
        item.LastUpdated,
        item.Status,
        item.Result,
        item.SourceJobPostingId);

    private static OnGoingJobResponse ToResponse(OnGoingJob item) => new(
        item.Id,
        item.Company,
        item.JobTitle,
        item.Link,
        item.Notes,
        item.CreatedAt);

    private static CareerSiteResponse ToResponse(CareerSite item) => new(
        item.Id,
        item.Name,
        item.Url,
        item.LastJobUpdated,
        item.LastCheckedAt);
}
