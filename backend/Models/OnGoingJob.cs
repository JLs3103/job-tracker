using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public sealed class OnGoingJob
{
    public int Id { get; set; }

    [Required, MaxLength(300)]
    public required string Company { get; set; }

    [Required, MaxLength(300)]
    public required string JobTitle { get; set; }

    [Required, MaxLength(2048)]
    public required string Link { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
