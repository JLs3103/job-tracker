using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace backend.Controllers;

/// <summary>Provides operations for aggregating and tracking job postings.</summary>
[ApiController]
[Route("api/jobs")]
[Produces("application/json")]
public sealed class JobsController(IJobService jobService) : ControllerBase
{
    /// <summary>Gets job postings with optional status, source, and text filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<JobPostingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<JobPostingResponse>>> GetJobs(
        [FromQuery] string? status,
        [FromQuery] string? source,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var jobs = await jobService.GetJobsAsync(
            status,
            source,
            search,
            cancellationToken);
        return Ok(jobs);
    }

    /// <summary>Gets the last successful refresh timestamp.</summary>
    [HttpGet("refresh-state")]
    [ProducesResponseType(typeof(RefreshStateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RefreshStateResponse>> GetRefreshState(
        CancellationToken cancellationToken)
    {
        var state = await jobService.GetRefreshStateAsync(cancellationToken);
        return Ok(state);
    }

    /// <summary>Fetches new postings, filters for IT roles, and stores them.</summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("job-refresh")]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<RefreshResponse>> Refresh(
        CancellationToken cancellationToken)
    {
        var result = await jobService.RefreshAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>Retries AI extraction for a posting that contains fallback details.</summary>
    [HttpPost("{id:int}/reextract")]
    [EnableRateLimiting("job-ai-retry")]
    [ProducesResponseType(typeof(JobPostingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<JobPostingResponse>> RetryExtraction(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var updatedJob = await jobService.RetryExtractionAsync(
            id,
            cancellationToken);
        return Ok(updatedJob);
    }

    /// <summary>Updates the tracking status of a job posting.</summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(JobPostingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobPostingResponse>> UpdateStatus(
        [FromRoute] int id,
        [FromBody] UpdateJobStatusRequest request,
        CancellationToken cancellationToken)
    {
        var updatedJob = await jobService.UpdateStatusAsync(
            id,
            request.Status,
            cancellationToken);
        return Ok(updatedJob);
    }
}
