// -----------------------------------------------------------------------
// <copyright file="CampaignCreatedEvent.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using OutreachGenie.Api.Domain.Abstractions;

namespace OutreachGenie.Api.Domain.Models;

/// <summary>
/// Event raised when a campaign is created.
/// </summary>
public sealed class CampaignCreatedEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CampaignCreatedEvent"/> class.
    /// </summary>
    public CampaignCreatedEvent(
        Guid campaignId,
        string name)
    {
        this.EventId = Guid.NewGuid();
        this.Timestamp = DateTime.UtcNow;
        this.CampaignId = campaignId;
        this.Name = name;
    }

    /// <inheritdoc />
    public Guid EventId { get; }

    /// <inheritdoc />
    public DateTime Timestamp { get; }

    /// <inheritdoc />
    public string EventType => nameof(CampaignCreatedEvent);

    /// <summary>
    /// Campaign identifier.
    /// </summary>
    public Guid CampaignId { get; }

    /// <summary>
    /// Campaign name.
    /// </summary>
    public string Name { get; }
}

