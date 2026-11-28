using System.Text.Json;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();

    public DbSet<JobRefreshState> JobRefreshStates => Set<JobRefreshState>();

    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    public DbSet<OnGoingJob> OnGoingJobs => Set<OnGoingJob>();

    public DbSet<CareerSite> CareerSites => Set<CareerSite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<JobPosting>();

        entity.HasKey(job => job.Id);
        entity.HasIndex(job => job.ExternalUrl).IsUnique();
        entity.HasIndex(job => new { job.Status, job.PublishedAt });

        entity.Property(job => job.ExternalUrl).HasMaxLength(2048).IsRequired();
        entity.Property(job => job.Source).HasConversion<string>().IsRequired();
        entity.Property(job => job.CompanyName).HasMaxLength(300).IsRequired();
        entity.Property(job => job.JobTitle).HasMaxLength(300).IsRequired();
        entity.Property(job => job.Location).HasMaxLength(300);
        entity.Property(job => job.MatchReason).HasMaxLength(500);
        entity.Property(job => job.Status).HasConversion<string>().IsRequired();
        ConfigureStringArray(entity.Property(job => job.Qualifications));
        ConfigureStringArray(entity.Property(job => job.TargetRoles));
        ConfigureStringArray(entity.Property(job => job.TechnicalSkills));
        entity.Property(job => job.PublishedAt)
            .HasConversion(
                value => value.ToUnixTimeMilliseconds(),
                value => DateTimeOffset.FromUnixTimeMilliseconds(value))
            .IsRequired();
        entity.Property(job => job.CreatedAt)
            .HasConversion(
                value => value.ToUnixTimeMilliseconds(),
                value => DateTimeOffset.FromUnixTimeMilliseconds(value))
            .IsRequired();

        var refreshState = modelBuilder.Entity<JobRefreshState>();
        refreshState.HasKey(state => state.Id);
        refreshState.Property(state => state.LastSuccessfulRefreshAt)
            .HasConversion(
                value => value.ToUnixTimeMilliseconds(),
                value => DateTimeOffset.FromUnixTimeMilliseconds(value))
            .IsRequired();

        var application = modelBuilder.Entity<JobApplication>();
        application.HasKey(item => item.Id);
        application.HasIndex(item => item.AppliedDate);
        application.HasIndex(item => item.SourceJobPostingId);
        application.Property(item => item.Company).HasMaxLength(300).IsRequired();
        application.Property(item => item.JobTitle).HasMaxLength(300).IsRequired();
        application.Property(item => item.AppliedVia).HasMaxLength(120).IsRequired();
        application.Property(item => item.AppliedLink).HasMaxLength(2048);
        application.Property(item => item.Status).HasMaxLength(120).IsRequired();
        application.Property(item => item.Result).HasConversion<string>().IsRequired();
        ConfigureUnixTimestamp(application.Property(item => item.AppliedDate));
        ConfigureUnixTimestamp(application.Property(item => item.LastUpdated));

        var onGoing = modelBuilder.Entity<OnGoingJob>();
        onGoing.HasKey(item => item.Id);
        onGoing.HasIndex(item => item.CreatedAt);
        onGoing.Property(item => item.Company).HasMaxLength(300).IsRequired();
        onGoing.Property(item => item.JobTitle).HasMaxLength(300).IsRequired();
        onGoing.Property(item => item.Link).HasMaxLength(2048).IsRequired();
        onGoing.Property(item => item.Notes).HasMaxLength(500);
        ConfigureUnixTimestamp(onGoing.Property(item => item.CreatedAt));

        var careerSite = modelBuilder.Entity<CareerSite>();
        careerSite.HasKey(item => item.Id);
        careerSite.Property(item => item.Name).HasMaxLength(300).IsRequired();
        careerSite.Property(item => item.Url).HasMaxLength(2048).IsRequired();
        careerSite.Property(item => item.LastJobUpdated).HasMaxLength(500);
        careerSite.Property(item => item.LastCheckedAt)
            .HasConversion(
                value => value.HasValue ? value.Value.ToUnixTimeMilliseconds() : (long?)null,
                value => value.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(value.Value) : null);
    }

    private static void ConfigureUnixTimestamp(PropertyBuilder<DateTimeOffset> property)
    {
        property
            .HasConversion(
                value => value.ToUnixTimeMilliseconds(),
                value => DateTimeOffset.FromUnixTimeMilliseconds(value))
            .IsRequired();
    }

    private static void ConfigureStringArray(PropertyBuilder<string[]> property)
    {
        property
            .HasConversion(
                items => JsonSerializer.Serialize(items),
                json => string.IsNullOrWhiteSpace(json)
                    ? Array.Empty<string>()
                    : JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>())
            .Metadata.SetValueComparer(
                new ValueComparer<string[]>(
                    (left, right) => left.SequenceEqual(right),
                    value => value.Aggregate(
                        0,
                        (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    value => value.ToArray()));
        property.IsRequired();
    }
}
