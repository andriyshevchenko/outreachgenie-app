namespace OutreachGenie.Api.Domain.Entities;

/// <summary>
/// Represents a persistent agent thread for state management.
/// </summary>
public sealed class AgentThread
{
    private AgentThread()
    {
        // Required for EF Core
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentThread"/> class.
    /// </summary>
    public AgentThread(
        Guid id,
        Guid campaignId,
        string threadId,
        string state,
        DateTime createdAt)
    {
        this.Id = id;
        this.CampaignId = campaignId;
        this.ThreadId = threadId;
        this.State = state;
        this.CreatedAt = createdAt;
        this.LastAccessedAt = createdAt;
    }

    /// <summary>
    /// Agent thread identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Campaign identifier.
    /// </summary>
    public Guid CampaignId { get; private set; }

    /// <summary>
    /// Agent Framework thread identifier.
    /// </summary>
    public string ThreadId { get; private set; } = string.Empty;

    /// <summary>
    /// Serialized state as JSON string.
    /// </summary>
    public string State { get; private set; } = string.Empty;

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last access timestamp.
    /// </summary>
    public DateTime LastAccessedAt { get; private set; }

    /// <summary>
    /// Parent campaign.
    /// </summary>
    public Campaign Campaign { get; private set; } = null!;
}
