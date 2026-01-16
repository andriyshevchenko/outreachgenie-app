namespace OutreachGenie.Api.Models;

/// <summary>
/// Campaign status data transfer object.
/// </summary>
public sealed class CampaignStatusDto
{
    /// <summary>
    /// Campaign identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Campaign name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current phase.
    /// </summary>
    public string Phase { get; set; } = string.Empty;

    /// <summary>
    /// Total tasks.
    /// </summary>
    public int TotalTasks { get; set; }

    /// <summary>
    /// Completed tasks.
    /// </summary>
    public int CompletedTasks { get; set; }

    /// <summary>
    /// Pending tasks.
    /// </summary>
    public int PendingTasks { get; set; }

    /// <summary>
    /// Total leads.
    /// </summary>
    public int TotalLeads { get; set; }

    /// <summary>
    /// Scored leads.
    /// </summary>
    public int ScoredLeads { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
