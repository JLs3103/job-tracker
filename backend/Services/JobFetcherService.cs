using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using backend.Models;

namespace backend.Services;

public sealed class JobFetcherService(
    HttpClient httpClient,
    ILogger<JobFetcherService> logger) : IJobFetcherService
{
    private static readonly IReadOnlyDictionary<JobSource, string> SourceUrls =
        new Dictionary<JobSource, string>
        {
            [JobSource.LokerBlog] =
                "https://www.lokerblog.com/feeds/posts/default?alt=json&max-results=15",
            [JobSource.Bukajobs] =
                "https://bukajobs.com/wp-json/wp/v2/posts?per_page=15",
            [JobSource.BrowseLoker] =
                "https://browseloker.lowongankerjacareer.com/wp-json/wp/v2/posts?per_page=15",
            [JobSource.Disnakerja] =
                "https://www.disnakerja.com/wp-json/wp/v2/posts?per_page=15"
        };

    private static readonly Regex ScriptAndStylePattern = new(
        @"<(script|style)\b[^>]*>[\s\S]*?</\1\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HtmlTagPattern = new(
        @"<[^>]+>",
        RegexOptions.Compiled);

    private static readonly Regex WhitespacePattern = new(
        @"\s+",
        RegexOptions.Compiled);

    public async Task<JobFetchResult> FetchAsync(
        CancellationToken cancellationToken)
    {
        var fetchTasks = SourceUrls.Select(pair =>
            FetchSourceAsync(pair.Key, pair.Value, cancellationToken));
        var results = await Task.WhenAll(fetchTasks);
        var failedSources = results
            .Where(result => result.Failure is not null)
            .Select(result => result.Source.ToString())
            .ToArray();
        var articles = results
            .Where(result => result.Failure is null)
            .SelectMany(result => result.Articles)
            .ToArray();

        if (failedSources.Length > 0 && articles.Length == 0)
        {
            throw new JobSourcesUnavailableException(
                $"All job sources failed: {string.Join(", ", failedSources)}.");
        }

        return new JobFetchResult(articles, failedSources);
    }

    private async Task<SourceFetchResult> FetchSourceAsync(
        JobSource source,
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream =
                await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document =
                await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var articles = source == JobSource.LokerBlog
                ? ParseBloggerFeed(document.RootElement, source)
                : ParseWordPressFeed(document.RootElement, source);

            return new SourceFetchResult(source, articles, null);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Unable to fetch job postings from {JobSource}",
                source);
            return new SourceFetchResult(source, [], exception);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Invalid JSON received from {JobSource}",
                source);
            return new SourceFetchResult(source, [], exception);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "Timed out while fetching job postings from {JobSource}",
                source);
            return new SourceFetchResult(source, [], exception);
        }
    }

    private static IReadOnlyList<JobArticle> ParseBloggerFeed(
        JsonElement root,
        JobSource source)
    {
        if (!root.TryGetProperty("feed", out var feed) ||
            feed.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("The Blogger feed object is missing.");
        }

        if (!feed.TryGetProperty("entry", out var entries) ||
            entries.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (entries.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("The Blogger entry list is invalid.");
        }

        var articles = new List<JobArticle>();
        foreach (var entry in entries.EnumerateArray())
        {
            var title = BloggerText(entry, "title");
            var content = BloggerText(entry, "content") ?? string.Empty;
            var externalUrl = ReadBloggerUrl(entry);
            if (string.IsNullOrWhiteSpace(title) || externalUrl is null)
            {
                throw new JsonException("A Blogger entry is missing its title or URL.");
            }

            articles.Add(new JobArticle(
                source,
                CleanText(title),
                CleanText(content),
                ValidateUrl(externalUrl),
                ParseDate(BloggerText(entry, "published")) ??
                DateTimeOffset.UtcNow));
        }

        return articles;
    }

    private static IReadOnlyList<JobArticle> ParseWordPressFeed(
        JsonElement root,
        JobSource source)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("The WordPress post list is invalid.");
        }

        var articles = new List<JobArticle>();
        foreach (var post in root.EnumerateArray())
        {
            var title = ReadNestedString(post, "title", "rendered");
            var content = ReadNestedString(post, "content", "rendered") ??
                string.Empty;
            var externalUrl = ReadString(post, "link");

            if (string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(externalUrl))
            {
                throw new JsonException(
                    $"A {source} post is missing its title or URL.");
            }

            articles.Add(new JobArticle(
                source,
                CleanText(title),
                CleanText(content),
                ValidateUrl(externalUrl),
                ParseDate(ReadString(post, "date_gmt")) ??
                ParseDate(ReadString(post, "date")) ??
                DateTimeOffset.UtcNow));
        }

        return articles;
    }

    private static string? BloggerText(JsonElement entry, string propertyName)
    {
        if (!entry.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Object ||
            !property.TryGetProperty("$t", out var text) ||
            text.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return text.GetString();
    }

    private static string? ReadBloggerUrl(JsonElement entry)
    {
        if (!entry.TryGetProperty("link", out var links) ||
            links.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var link in links.EnumerateArray())
        {
            if (ReadString(link, "rel") == "alternate")
            {
                return ReadString(link, "href");
            }
        }

        return null;
    }

    private static string? ReadNestedString(
        JsonElement element,
        string parentProperty,
        string childProperty)
    {
        if (!element.TryGetProperty(parentProperty, out var parent) ||
            parent.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return ReadString(parent, childProperty);
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
    }

    private static string CleanText(string value)
    {
        var withoutScriptAndStyle = ScriptAndStylePattern.Replace(value, " ");
        var withoutTags = HtmlTagPattern.Replace(withoutScriptAndStyle, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return WhitespacePattern.Replace(decoded, " ").Trim();
    }

    private static string ValidateUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new JsonException("A job source returned an invalid URL.");
        }

        return uri.ToString();
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
                ? parsed
                : null;
    }

    private sealed record SourceFetchResult(
        JobSource Source,
        IReadOnlyList<JobArticle> Articles,
        Exception? Failure);
}
