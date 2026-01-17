// -----------------------------------------------------------------------
// <copyright file="LeadScoredEvent.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

// -----------------------------------------------------------------------
// <copyright file="LeadScoredEvent.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using OutreachGenie.Api.Domain.Abstractions;

namespace OutreachGenie.Api.Domain.Models;

/// <summary>
/// Event logged when a lead is scored.
/// </summary>
internal sealed record LeadScoredEvent(
    Guid LeadId,
    Guid CampaignId,
    decimal Score,
    string Rationale) : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <inheritdoc />
    public string EventType => nameof(LeadScoredEvent);
}

