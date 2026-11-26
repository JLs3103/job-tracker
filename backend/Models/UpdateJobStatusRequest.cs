using System.ComponentModel.DataAnnotations;

namespace backend.Models;

/// <summary>Updates the tracking status of a job posting.</summary>
public sealed record UpdateJobStatusRequest
{
    /// <summary>The new tracking status for the job posting.</summary>
    [Required, EnumDataType(typeof(JobStatus))]
    public required JobStatus Status { get; init; }
}
