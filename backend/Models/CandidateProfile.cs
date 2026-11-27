using System.ComponentModel.DataAnnotations;

namespace backend.Models;

/// <summary>Represents the user's CV/resume profile used for job match scoring.</summary>
public sealed record CandidateProfile
{
    public IReadOnlyList<string> TargetRoles { get; init; } = [];

    public IReadOnlyList<string> TechnicalSkills { get; init; } = [];

    [Required, MaxLength(120)]
    public string ExperienceLevel { get; init; } = "Fresh Graduate";

    [MaxLength(2000)]
    public string Summary { get; init; } = string.Empty;
}
