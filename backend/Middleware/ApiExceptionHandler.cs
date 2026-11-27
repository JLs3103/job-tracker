using backend.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace backend.Middleware;

internal sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            ArgumentException =>
                (StatusCodes.Status400BadRequest, "Invalid request", "One or more request filters are invalid."),
            KeyNotFoundException =>
                (StatusCodes.Status404NotFound, "Not Found", "The requested job posting was not found."),
            JobSourcesUnavailableException =>
                (StatusCodes.Status503ServiceUnavailable, "Job sources unavailable", "Job sources are temporarily unavailable."),
            JobArticleUnavailableException =>
                (StatusCodes.Status422UnprocessableEntity, "Sumber lowongan tidak tersedia", "Artikel tidak lagi ditemukan di feed sumber. Buka tautan sumber untuk memeriksanya."),
            AiExtractionUnavailableException =>
                (StatusCodes.Status502BadGateway, "Ekstraksi AI gagal", "Gemini belum berhasil mengambil rincian lowongan. Periksa model dan API key, lalu coba lagi. Data yang tersimpan tidak diubah."),
            _ => (0, string.Empty, string.Empty)
        };

        if (statusCode == 0)
        {
            return false;
        }

        logger.LogWarning(
            exception,
            "Handled API exception with status {StatusCode}",
            statusCode);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            },
            cancellationToken);

        return true;
    }
}
