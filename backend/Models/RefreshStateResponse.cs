namespace backend.Models;

/// <summary>Represents the persisted refresh checkpoint state.</summary>
public sealed record RefreshStateResponse(
    DateTimeOffset? LastSuccessfulRefreshAt);
