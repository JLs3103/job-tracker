namespace backend.Services;

public sealed class CvTextUnavailableException(string message) : Exception(message)
{
}
