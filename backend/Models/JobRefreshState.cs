namespace backend.Models;

public sealed class JobRefreshState
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public DateTimeOffset LastSuccessfulRefreshAt { get; set; }
}
