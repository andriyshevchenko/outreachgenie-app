using OutreachGenie.Domain.Enums;

namespace OutreachGenie.Domain.Entities;

/// <summary>
/// Represents a LinkedIn outreach campaign with deterministic state management.
/// </summary>
public sealed class Campaign
{
    /// <summary>
    /// Gets or sets the unique identifier for this campaign.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user-defined campaign name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current lifecycle state of the campaign.
    /// </summary>
    public CampaignStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the description of target audience for this campaign.
    /// </summary>
    public string TargetAudience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the campaign was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last state update.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the campaign completed or failed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the agent working directory path for this campaign.
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets the collection of tasks associated with this campaign.
    /// </summary>
    public ICollection<CampaignTask> Tasks { get; } = new List<CampaignTask>();

    /// <summary>
    /// Gets the collection of artifacts generated during this campaign.
    /// </summary>
    public ICollection<Artifact> Artifacts { get; } = new List<Artifact>();

    /// <summary>
    /// Gets the collection of leads targeted by this campaign.
    /// </summary>
    public ICollection<Lead> Leads { get; } = new List<Lead>();
}
