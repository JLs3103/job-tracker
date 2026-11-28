using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/// <summary>Manages tracked job applications, manual on-going bookmarks, and career sites.</summary>
[ApiController]
[Route("api/career")]
[Produces("application/json")]
public sealed class CareerTrackerController(ICareerTrackerService careerService) : ControllerBase
{
    [HttpGet("applications")]
    [ProducesResponseType(typeof(IReadOnlyList<JobApplicationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobApplicationResponse>>> GetApplications(
        [FromQuery] ApplicationResult? result,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        return Ok(await careerService.GetApplicationsAsync(result, search, cancellationToken));
    }

    [HttpPost("applications")]
    [ProducesResponseType(typeof(JobApplicationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobApplicationResponse>> CreateApplication(
        [FromBody] CreateJobApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var created = await careerService.CreateApplicationAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetApplications), new { id = created.Id }, created);
    }

    [HttpPatch("applications/{id:int}")]
    [ProducesResponseType(typeof(JobApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobApplicationResponse>> UpdateApplication(
        [FromRoute] int id,
        [FromBody] UpdateJobApplicationRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await careerService.UpdateApplicationAsync(id, request, cancellationToken));
    }

    [HttpDelete("applications/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteApplication(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        await careerService.DeleteApplicationAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("ongoing")]
    [ProducesResponseType(typeof(IReadOnlyList<OnGoingJobResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OnGoingJobResponse>>> GetOnGoing(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        return Ok(await careerService.GetOnGoingAsync(search, cancellationToken));
    }

    [HttpPost("ongoing")]
    [ProducesResponseType(typeof(OnGoingJobResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<OnGoingJobResponse>> CreateOnGoing(
        [FromBody] CreateOnGoingJobRequest request,
        CancellationToken cancellationToken)
    {
        var created = await careerService.CreateOnGoingAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetOnGoing), new { id = created.Id }, created);
    }

    [HttpPatch("ongoing/{id:int}")]
    [ProducesResponseType(typeof(OnGoingJobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OnGoingJobResponse>> UpdateOnGoing(
        [FromRoute] int id,
        [FromBody] UpdateOnGoingJobRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await careerService.UpdateOnGoingAsync(id, request, cancellationToken));
    }

    [HttpPost("ongoing/{id:int}/apply")]
    [ProducesResponseType(typeof(JobApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobApplicationResponse>> MoveOnGoingToApplied(
        [FromRoute] int id,
        [FromBody] MoveOnGoingToAppliedRequest? request,
        CancellationToken cancellationToken)
    {
        return Ok(await careerService.MoveOnGoingToAppliedAsync(id, request?.AppliedVia, cancellationToken));
    }

    [HttpDelete("ongoing/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOnGoing(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        await careerService.DeleteOnGoingAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("sites")]
    [ProducesResponseType(typeof(IReadOnlyList<CareerSiteResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CareerSiteResponse>>> GetCareerSites(
        CancellationToken cancellationToken)
    {
        return Ok(await careerService.GetCareerSitesAsync(cancellationToken));
    }

    [HttpPost("sites")]
    [ProducesResponseType(typeof(CareerSiteResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CareerSiteResponse>> CreateCareerSite(
        [FromBody] CreateCareerSiteRequest request,
        CancellationToken cancellationToken)
    {
        var created = await careerService.CreateCareerSiteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCareerSites), new { id = created.Id }, created);
    }

    [HttpPatch("sites/{id:int}")]
    [ProducesResponseType(typeof(CareerSiteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CareerSiteResponse>> UpdateCareerSite(
        [FromRoute] int id,
        [FromBody] UpdateCareerSiteRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await careerService.UpdateCareerSiteAsync(id, request, cancellationToken));
    }

    [HttpDelete("sites/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCareerSite(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        await careerService.DeleteCareerSiteAsync(id, cancellationToken);
        return NoContent();
    }
}
