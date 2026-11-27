using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/// <summary>Provides operations to view, update, and parse the candidate CV profile.</summary>
[ApiController]
[Route("api/profile")]
[Produces("application/json")]
public sealed class ProfileController(
    IProfileService profileService,
    IAiExtractorService aiExtractor) : ControllerBase
{
    private const long MaxCvBytes = 5 * 1024 * 1024;

    /// <summary>Gets the current candidate profile.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CandidateProfile), StatusCodes.Status200OK)]
    public async Task<ActionResult<CandidateProfile>> GetProfile(
        CancellationToken cancellationToken)
    {
        var profile = await profileService.GetProfileAsync(cancellationToken);
        return Ok(profile);
    }

    /// <summary>Updates the candidate profile used for job match scoring.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(CandidateProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CandidateProfile>> UpdateProfile(
        [FromBody] CandidateProfile request,
        CancellationToken cancellationToken)
    {
        var updated = await profileService.UpdateProfileAsync(
            request,
            cancellationToken);
        return Ok(updated);
    }

    /// <summary>Parses a PDF CV/resume with AI into editable candidate profile fields.</summary>
    [HttpPost("parse-cv")]
    [RequestSizeLimit(MaxCvBytes)]
    [ProducesResponseType(typeof(CandidateProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<CandidateProfile>> ParseCv(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("Pilih file CV berformat PDF terlebih dahulu.");
        }

        if (file.Length > MaxCvBytes)
        {
            throw new ArgumentException("Ukuran file CV maksimal 5 MB.");
        }

        if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Format file harus PDF (.pdf).");
        }

        await using var stream = file.OpenReadStream();
        var cvText = PdfTextExtractor.ExtractText(stream);
        var parsedProfile = await aiExtractor.ExtractProfileFromCvAsync(
            cvText,
            cancellationToken);
        return Ok(parsedProfile);
    }
}
