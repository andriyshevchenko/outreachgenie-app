namespace OutreachGenie.Api.Domain.Abstractions;

/// <summary>
/// Marker interface for domain events.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Type of the event.
    /// </summary>
    string EventType { get; }
}
