namespace OutreachGenie.Api.Domain.Entities;

/// <summary>
/// Represents the status of a campaign task.
/// </summary>
public enum TaskStatus
{
    /// <summary>Task created but not started.</summary>
    Pending,

    /// <summary>Task currently being executed.</summary>
    InProgress,

    /// <summary>Task finished successfully.</summary>
    Completed,

    /// <summary>Task blocked by dependencies.</summary>
    Blocked
}
