using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public sealed class JobPosting
{
    public int Id { get; set; }

    [Required, MaxLength(2048)]
    public required string ExternalUrl { get; set; }

    public JobSource Source { get; set; }

    [Required, MaxLength(300)]
    public required string CompanyName { get; set; }

    [Required, MaxLength(300)]
    public required string JobTitle { get; set; }

    public string[] Qualifications { get; set; } = [];

    [MaxLength(300)]
    public string? Location { get; set; }

    public JobStatus Status { get; set; } = JobStatus.NEW;

    public DateTimeOffset PublishedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
