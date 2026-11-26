using System.Text.Json;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace backend.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();

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
        entity.Property(job => job.Status).HasConversion<string>().IsRequired();
        var qualificationsProperty = entity.Property(job => job.Qualifications)
            .HasConversion(
                qualifications => JsonSerializer.Serialize(qualifications),
                json => JsonSerializer.Deserialize<string[]>(json) ??
                    Array.Empty<string>());
        qualificationsProperty.Metadata.SetValueComparer(
            new ValueComparer<string[]>(
                (left, right) => left.SequenceEqual(right),
                value => value.Aggregate(
                    0,
                    (hash, qualification) =>
                        HashCode.Combine(hash, qualification.GetHashCode())),
                value => value.ToArray()));
        qualificationsProperty.IsRequired();
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
    }
}
