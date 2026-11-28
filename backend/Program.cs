using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using backend;
using backend.Data;
using backend.Middleware;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

const string StandaloneUrl = "http://localhost:5173";
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")) &&
    !builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls(StandaloneUrl);
}

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddCors(options =>
    options.AddPolicy(
        "Frontend",
        policy => policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(
        "job-refresh",
        limiter =>
        {
            limiter.PermitLimit = 1;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
            limiter.AutoReplenishment = true;
        });
    options.AddFixedWindowLimiter(
        "job-ai-retry",
        limiter =>
        {
            limiter.PermitLimit = 5;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
            limiter.AutoReplenishment = true;
        });
});
var configuredConnectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");
var defaultDbPath = Path.Combine(AppContext.BaseDirectory, "jobs.db");
var resolvedConnectionString = string.IsNullOrWhiteSpace(configuredConnectionString)
    ? $"Data Source={defaultDbPath}"
    : configuredConnectionString;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(resolvedConnectionString));
builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IProfileService, ProfileService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<ICareerTrackerService, CareerTrackerService>();
builder.Services.AddHttpClient<IJobFetcherService, JobFetcherService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
});
builder.Services.AddHttpClient("Ollama", client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
});
builder.Services.AddHttpClient("Gemini", client =>
{
    client.Timeout = TimeSpan.FromSeconds(35);
});
builder.Services.AddScoped<IAiExtractorService, AiExtractorService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthorization();

var hasEmbeddedFrontend = File.Exists(
    Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "index.html"));
var isStandaloneApp = hasEmbeddedFrontend && !app.Environment.IsDevelopment();

if (isStandaloneApp)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.MapControllers();

if (isStandaloneApp)
{
    app.MapFallbackToFile("index.html");
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        TrayHost.Start(StandaloneUrl, app.Lifetime);
        TrayHost.OpenBrowser(StandaloneUrl);
    });
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

try
{
    app.Run();
}
catch (IOException) when (isStandaloneApp)
{
    // If already running on localhost:5173, open the existing instance in the browser.
    TrayHost.OpenBrowser(StandaloneUrl);
}
catch (AddressInUseException) when (isStandaloneApp)
{
    TrayHost.OpenBrowser(StandaloneUrl);
}
