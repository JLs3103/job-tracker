using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public sealed class JobApplication
{
    public int Id { get; set; }

    [Required, MaxLength(300)]
    public required string Company { get; set; }

    [Required, MaxLength(300)]
    public required string JobTitle { get; set; }

    [Required, MaxLength(120)]
    public required string AppliedVia { get; set; }

    [MaxLength(2048)]
    public string? AppliedLink { get; set; }

    public DateTimeOffset AppliedDate { get; set; }

    public DateTimeOffset LastUpdated { get; set; }

    [Required, MaxLength(120)]
    public string Status { get; set; } = "Applied";

    public ApplicationResult Result { get; set; } = ApplicationResult.InProgress;

    public int? SourceJobPostingId { get; set; }
}
