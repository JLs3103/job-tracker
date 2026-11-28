using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public sealed class CareerSite
{
    public int Id { get; set; }

    [Required, MaxLength(300)]
    public required string Name { get; set; }

    [Required, MaxLength(2048)]
    public required string Url { get; set; }

    [MaxLength(500)]
    public string? LastJobUpdated { get; set; }

    public DateTimeOffset? LastCheckedAt { get; set; }
}
