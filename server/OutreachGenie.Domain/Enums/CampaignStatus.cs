namespace OutreachGenie.Domain.Enums;

/// <summary>
/// Represents the lifecycle state of a campaign.
/// </summary>
public enum CampaignStatus
{
    /// <summary>
    /// Campaign is being set up, not yet ready for execution.
    /// </summary>
    Initializing,

    /// <summary>
    /// Campaign is actively running and processing tasks.
    /// </summary>
    Active,

    /// <summary>
    /// Campaign execution is temporarily paused by user.
    /// </summary>
    Paused,

    /// <summary>
    /// All campaign tasks completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Campaign encountered unrecoverable error.
    /// </summary>
    Error,
}
