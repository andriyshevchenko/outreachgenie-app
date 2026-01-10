namespace OutreachGenie.Domain.Enums;

/// <summary>
/// Represents the execution state of a task within a campaign.
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Task is queued awaiting execution.
    /// </summary>
    Pending,

    /// <summary>
    /// Task is currently being executed by the controller.
    /// </summary>
    InProgress,

    /// <summary>
    /// Task completed successfully with verified side effects.
    /// </summary>
    Done,

    /// <summary>
    /// Task cannot proceed due to unmet preconditions or dependencies.
    /// </summary>
    Blocked,

    /// <summary>
    /// Task execution failed and is being retried.
    /// </summary>
    Retrying,

    /// <summary>
    /// Task failed after exhausting retry attempts.
    /// </summary>
    Failed,
}
