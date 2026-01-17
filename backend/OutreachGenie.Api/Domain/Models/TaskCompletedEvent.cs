// -----------------------------------------------------------------------
// <copyright file="TaskCompletedEvent.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using OutreachGenie.Api.Domain.Abstractions;

namespace OutreachGenie.Api.Domain.Models;

/// <summary>
/// Event raised when a task is completed.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated public classes", Justification = "Instantiated at runtime when tasks are completed")]
public sealed class TaskCompletedEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskCompletedEvent"/> class.
    /// </summary>
    public TaskCompletedEvent(
        Guid taskId,
        Guid campaignId,
        string title,
        string completedBy)
    {
        this.EventId = Guid.NewGuid();
        this.Timestamp = DateTime.UtcNow;
        this.TaskId = taskId;
        this.CampaignId = campaignId;
        this.Title = title;
        this.CompletedBy = completedBy;
    }

    /// <inheritdoc />
    public Guid EventId { get; }

    /// <inheritdoc />
    public DateTime Timestamp { get; }

    /// <inheritdoc />
    public string EventType => nameof(TaskCompletedEvent);

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
    /// Who completed the task.
    /// </summary>
    public string CompletedBy { get; }
}

