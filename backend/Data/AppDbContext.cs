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
