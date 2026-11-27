using System.Text.Json;
using backend.Models;

namespace backend.Services;

public sealed class ProfileService(
    IHostEnvironment environment,
    ILogger<ProfileService> logger) : IProfileService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static readonly CandidateProfile DefaultProfile = new()
    {
        TargetRoles =
        [
            "Full Stack Developer",
            "Frontend Developer",
            "IT Security / Blue Team",
            "SOC Analyst",
            "IT Support"
        ],
        TechnicalSkills =
        [
            "React",
            "TypeScript",
            "C#",
            ".NET",
            "Linux",
            "Jaringan",
            "SOC"
        ],
        ExperienceLevel = "Fresh Graduate",
        Summary = "Lulusan baru IT dengan fokus Full Stack, Frontend, IT Security / Blue Team, dan IT Support."
    };

    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly string _profilePath = Path.Combine(
        environment.ContentRootPath,
        "Data",
        "profile.json");

    public async Task<CandidateProfile> GetProfileAsync(
        CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_profilePath))
            {
                return DefaultProfile;
            }

            await using var stream = File.OpenRead(_profilePath);
            var profile = await JsonSerializer.DeserializeAsync<CandidateProfile>(
                stream,
                JsonOptions,
                cancellationToken);

            return profile is null ? DefaultProfile : Normalize(profile);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Failed to parse profile.json at {ProfilePath}; falling back to default profile.",
                _profilePath);
            return DefaultProfile;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<CandidateProfile> UpdateProfileAsync(
        CandidateProfile profile,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(profile);
        var directory = Path.GetDirectoryName(_profilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(normalized, JsonOptions);
            await File.WriteAllTextAsync(_profilePath, json, cancellationToken);
            return normalized;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private static CandidateProfile Normalize(CandidateProfile profile)
    {
        return new CandidateProfile
        {
            TargetRoles = profile.TargetRoles
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToArray(),
            TechnicalSkills = profile.TechnicalSkills
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(40)
                .ToArray(),
            ExperienceLevel = string.IsNullOrWhiteSpace(profile.ExperienceLevel)
                ? "Fresh Graduate"
                : profile.ExperienceLevel.Trim(),
            Summary = profile.Summary?.Trim() ?? string.Empty
        };
    }
}
