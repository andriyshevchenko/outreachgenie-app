using OutreachGenie.Api.Domain.Abstractions;

namespace OutreachGenie.Api.Domain.Models;

/// <summary>
/// Event raised when leads are discovered.
/// </summary>
public sealed class LeadsDiscoveredEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LeadsDiscoveredEvent"/> class.
    /// </summary>
    public LeadsDiscoveredEvent(
        Guid campaignId,
        int count,
        string source)
    {
        this.EventId = Guid.NewGuid();
        this.Timestamp = DateTime.UtcNow;
        this.CampaignId = campaignId;
        this.Count = count;
        this.Source = source;
    }

    /// <inheritdoc />
    public Guid EventId { get; }

    /// <inheritdoc />
    public DateTime Timestamp { get; }

    /// <inheritdoc />
    public string EventType => nameof(LeadsDiscoveredEvent);

    /// <summary>
    /// Campaign identifier.
    /// </summary>
    public Guid CampaignId { get; }

    /// <summary>
    /// Number of leads discovered.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Source of leads.
    /// </summary>
    public string Source { get; }
}
