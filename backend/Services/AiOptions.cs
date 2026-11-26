namespace backend.Services;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string? GeminiApiKey { get; set; }

    public string GeminiModel { get; set; } = "gemini-2.5-flash";
}
