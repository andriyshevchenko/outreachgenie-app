using OutreachGenie.Domain.Enums;

namespace OutreachGenie.Domain.Entities;

/// <summary>
/// Represents a discrete unit of work within a campaign execution flow.
/// </summary>
public sealed class CampaignTask
{
    /// <summary>
    /// Gets or sets the unique identifier for this task.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the campaign this task belongs to.
    /// </summary>
    public Guid CampaignId { get; set; }

    /// <summary>
    /// Gets or sets the human-readable description of what this task does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current execution state of this task.
    /// </summary>
    public Enums.TaskStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the type identifier for this task (e.g., "search_prospects", "send_message").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized input parameters required for task execution.
    /// </summary>
    public string? InputJson { get; set; }

    /// <summary>
    /// Gets or sets the serialized output result after successful execution.
    /// </summary>
    public string? OutputJson { get; set; }

    /// <summary>
    /// Gets or sets the number of times this task has been retried after failure.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts before marking as failed.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the error message if task failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the task was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when task execution started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the task completed or failed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the associated campaign entity.
    /// </summary>
    public Campaign Campaign { get; set; } = null!;
}
