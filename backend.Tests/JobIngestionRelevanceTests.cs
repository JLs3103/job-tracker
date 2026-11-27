using System.Net;
using System.Text;
using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace backend.Tests;

public sealed class JobIngestionRelevanceTests
{
    [Fact]
    public async Task AiExtractor_ReturnsNonItWithoutExtraGeminiCall_WhenOllamaMarksIsItJobFalse()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "message": {
                        "content": "{\"isItJob\":false,\"companyName\":\"PT Niaga\",\"jobTitle\":\"Bisnis Digital\",\"qualifications\":[],\"location\":\"\"}"
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            });

        var factory = new StubHttpClientFactory(new HttpClient(handler));
        var extractor = new AiExtractorService(
            factory,
            new StubProfileService(DefaultTestProfile()),
            Options.Create(new AiOptions { GeminiApiKey = "test-key" }),
            NullLogger<AiExtractorService>.Instance);

        var extraction = await extractor.ExtractAsync(
            new JobArticle(
                JobSource.Disnakerja,
                "Bisnis Digital",
                "Mengelola strategi pemasaran dan operasional bisnis digital.",
                "https://example.com/bisnis-digital",
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.False(extraction.IsItJob);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task RefreshAsync_SkipsNonItExtraction_AndPreservesExistingDatabaseRows()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.JobPostings.Add(new JobPosting
        {
            ExternalUrl = "https://example.com/existing-old-job",
            Source = JobSource.LokerBlog,
            CompanyName = "PT Lama",
            JobTitle = "Old Entry",
            Qualifications = ["Existing qualification"],
            Status = JobStatus.SAVED,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-2),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
        });
        await dbContext.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var fetcher = new StubJobFetcherService(new JobFetchResult(
            [
                new JobArticle(
                    JobSource.Disnakerja,
                    "IT Support",
                    "Troubleshooting hardware, jaringan, dan helpdesk.",
                    "https://example.com/it-support",
                    now.AddMinutes(-5)),
                new JobArticle(
                    JobSource.Bukajobs,
                    "Software QA Engineer",
                    "Menyusun test case dan automated testing.",
                    "https://example.com/false-positive-qa",
                    now.AddMinutes(-4))
            ],
            []));

        var extractor = new StubAiExtractorService(article =>
            article.ExternalUrl.EndsWith("it-support", StringComparison.Ordinal)
                ? new JobExtraction(
                    "PT Tech",
                    "IT Support",
                    ["Helpdesk & jaringan"],
                    "Jakarta",
                    IsItJob: true,
                    MatchScore: 88,
                    MatchReason: "Cocok dengan target IT Support dan skill jaringan.",
                    TargetRoles: ["IT Support"],
                    TechnicalSkills: ["Helpdesk", "Jaringan"])
                : new JobExtraction(string.Empty, string.Empty, [], null, IsItJob: false));

        var service = new JobService(
            dbContext,
            fetcher,
            extractor,
            TimeProvider.System,
            NullLogger<JobService>.Instance);

        var refresh = await service.RefreshAsync(CancellationToken.None);

        Assert.Equal(1, refresh.NewJobsCount);
        var savedJobs = await dbContext.JobPostings.OrderBy(j => j.Id).ToListAsync();
        Assert.Equal(2, savedJobs.Count);
        Assert.Contains(savedJobs, j => j.ExternalUrl == "https://example.com/existing-old-job");
        var itJob = savedJobs.Single(j => j.ExternalUrl == "https://example.com/it-support");
        Assert.Equal(88, itJob.MatchScore);
        Assert.Equal("Cocok dengan target IT Support dan skill jaringan.", itJob.MatchReason);
        Assert.Equal(["IT Support"], itJob.TargetRoles);
        Assert.Equal(["Helpdesk", "Jaringan"], itJob.TechnicalSkills);
        Assert.DoesNotContain(savedJobs, j => j.ExternalUrl == "https://example.com/false-positive-qa");

        var refreshState = await service.GetRefreshStateAsync(CancellationToken.None);
        Assert.NotNull(refreshState.LastSuccessfulRefreshAt);
    }

    [Fact]
    public async Task AiExtractor_IncludesProfileAndExtractsMatchScoreAndReason()
    {
        string? capturedBody = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "message": {
                        "content": "{\"isItJob\":true,\"companyName\":\"PT Web\",\"jobTitle\":\"Frontend Developer\",\"qualifications\":[\"React\",\"TypeScript\"],\"location\":\"Jakarta\",\"matchScore\":92,\"matchReason\":\"Sangat cocok dengan skill React dan TypeScript.\",\"targetRoles\":[\"Frontend Developer\"],\"technicalSkills\":[\"React\",\"TypeScript\"]}"
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var factory = new StubHttpClientFactory(new HttpClient(handler));
        var extractor = new AiExtractorService(
            factory,
            new StubProfileService(DefaultTestProfile()),
            Options.Create(new AiOptions()),
            NullLogger<AiExtractorService>.Instance);

        var extraction = await extractor.ExtractAsync(
            new JobArticle(
                JobSource.Disnakerja,
                "Frontend Developer",
                "Menguasai React dan TypeScript untuk aplikasi web.",
                "https://example.com/frontend",
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.True(extraction.IsItJob);
        Assert.Equal(92, extraction.MatchScore);
        Assert.Equal("Sangat cocok dengan skill React dan TypeScript.", extraction.MatchReason);
        Assert.Equal(["Frontend Developer"], extraction.TargetRoles);
        Assert.Equal(["React", "TypeScript"], extraction.TechnicalSkills);
        Assert.NotNull(capturedBody);
        Assert.Contains("React", capturedBody);
        Assert.Contains("Fresh Graduate", capturedBody);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task AiExtractor_ExtractProfileFromCvAsync_ParsesStructuredProfile()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "message": {
                        "content": "{\"targetRoles\":[\"Backend Developer\",\"DevOps Engineer\"],\"technicalSkills\":[\"C#\",\".NET\",\"Docker\"],\"experienceLevel\":\"Junior\",\"summary\":\"Pengembang backend .NET dengan pengalaman Docker.\"}"
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            });

        var factory = new StubHttpClientFactory(new HttpClient(handler));
        var extractor = new AiExtractorService(
            factory,
            new StubProfileService(DefaultTestProfile()),
            Options.Create(new AiOptions()),
            NullLogger<AiExtractorService>.Instance);

        var profile = await extractor.ExtractProfileFromCvAsync(
            "CV Kandidat: Menguasai C#, .NET Core, dan Docker.",
            CancellationToken.None);

        Assert.Equal(["Backend Developer", "DevOps Engineer"], profile.TargetRoles);
        Assert.Equal(["C#", ".NET", "Docker"], profile.TechnicalSkills);
        Assert.Equal("Junior", profile.ExperienceLevel);
        Assert.Equal("Pengembang backend .NET dengan pengalaman Docker.", profile.Summary);
    }

    [Fact]
    public void PdfTextExtractor_RejectsNonPdfStream()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not a pdf"));
        Assert.Throws<ArgumentException>(() => PdfTextExtractor.ExtractText(stream));
    }

    private static CandidateProfile DefaultTestProfile() => new()
    {
        TargetRoles = ["Full Stack Developer", "Frontend Developer", "IT Security"],
        TechnicalSkills = ["React", "TypeScript", "C#", ".NET", "Linux"],
        ExperienceLevel = "Fresh Graduate",
        Summary = "Fresh graduate IT."
    };

    private sealed class StubProfileService(CandidateProfile profile) : IProfileService
    {
        public Task<CandidateProfile> GetProfileAsync(CancellationToken cancellationToken) =>
            Task.FromResult(profile);

        public Task<CandidateProfile> UpdateProfileAsync(
            CandidateProfile updated,
            CancellationToken cancellationToken) => Task.FromResult(updated);
    }

    private sealed class StubJobFetcherService(JobFetchResult result) : IJobFetcherService
    {
        public Task<JobFetchResult> FetchAsync(
            CancellationToken cancellationToken,
            DateTimeOffset? publishedSince = null,
            JobSource? source = null) => Task.FromResult(result);
    }

    private sealed class StubAiExtractorService(Func<JobArticle, JobExtraction> extract)
        : IAiExtractorService
    {
        public Task<JobExtraction> ExtractAsync(
            JobArticle article,
            CancellationToken cancellationToken) => Task.FromResult(extract(article));

        public Task<CandidateProfile> ExtractProfileFromCvAsync(
            string cvText,
            CancellationToken cancellationToken) =>
            Task.FromResult(DefaultTestProfile());
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(responder(request));
        }
    }
}
