namespace backend.Services;

public sealed class JobSourcesUnavailableException(string message)
    : Exception(message)
{
}
