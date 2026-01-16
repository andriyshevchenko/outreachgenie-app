using OutreachGenie.Api.Domain.Abstractions;

namespace OutreachGenie.Api.Domain.Models;

/// <summary>
/// Event raised when a task is created.
/// </summary>
public sealed class TaskCreatedEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskCreatedEvent"/> class.
    /// </summary>
    public TaskCreatedEvent(
        Guid taskId,
        Guid campaignId,
        string title,
        int orderIndex)
    {
        this.EventId = Guid.NewGuid();
        this.Timestamp = DateTime.UtcNow;
        this.TaskId = taskId;
        this.CampaignId = campaignId;
        this.Title = title;
        this.OrderIndex = orderIndex;
    }

    /// <inheritdoc />
    public Guid EventId { get; }

    /// <inheritdoc />
    public DateTime Timestamp { get; }

    /// <inheritdoc />
    public string EventType => nameof(TaskCreatedEvent);

    /// <summary>
    /// Task identifier.
    /// </summary>
    public Guid TaskId { get; }

    /// <summary>
    /// Campaign identifier.
    /// </summary>
    public Guid CampaignId { get; }

    /// <summary>
    /// Task title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Order index.
    /// </summary>
    public int OrderIndex { get; }
}
