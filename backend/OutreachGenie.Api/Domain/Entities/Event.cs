namespace OutreachGenie.Api.Domain.Entities;

/// <summary>
/// Represents a domain event in the audit log.
/// </summary>
public sealed class Event
{
    private Event()
    {
        // Required for EF Core
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Event"/> class.
    /// </summary>
    public Event(
        Guid id,
        string eventType,
        Guid? campaignId,
        DateTime timestamp,
        EventActor actor,
        string payload)
    {
        this.Id = id;
        this.EventType = eventType;
        this.CampaignId = campaignId;
        this.Timestamp = timestamp;
        this.Actor = actor;
        this.Payload = payload;
    }

    /// <summary>
    /// Event identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Event type name.
    /// </summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// Campaign identifier.
    /// </summary>
    public Guid? CampaignId { get; private set; }

    /// <summary>
    /// Event timestamp.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Actor who triggered the event.
    /// </summary>
    public EventActor Actor { get; private set; }

    /// <summary>
    /// Event payload as JSON string.
    /// </summary>
    public string Payload { get; private set; } = string.Empty;
}
