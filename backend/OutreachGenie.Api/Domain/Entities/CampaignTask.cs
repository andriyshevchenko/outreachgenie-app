namespace OutreachGenie.Api.Domain.Entities;

/// <summary>
/// Represents a task within a campaign.
/// </summary>
public sealed class CampaignTask
{
    private CampaignTask()
    {
        // Required for EF Core
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CampaignTask"/> class.
    /// </summary>
    public CampaignTask(
        Guid id,
        Guid campaignId,
        string title,
        string description,
        TaskStatus status,
        int orderIndex,
        bool requiresApproval,
        DateTime createdAt)
    {
        this.Id = id;
        this.CampaignId = campaignId;
        this.Title = title;
        this.Description = description;
        this.Status = status;
        this.OrderIndex = orderIndex;
        this.RequiresApproval = requiresApproval;
        this.CreatedAt = createdAt;
    }

    /// <summary>
    /// Task identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Campaign identifier.
    /// </summary>
    public Guid CampaignId { get; private set; }

    /// <summary>
    /// Task title.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Task description.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Current status.
    /// </summary>
    public TaskStatus Status { get; private set; }

    /// <summary>
    /// Execution order index.
    /// </summary>
    public int OrderIndex { get; private set; }

    /// <summary>
    /// Indicates whether approval is required.
    /// </summary>
    public bool RequiresApproval { get; private set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Completion timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Parent campaign.
    /// </summary>
    public Campaign Campaign { get; private set; } = null!;
}
