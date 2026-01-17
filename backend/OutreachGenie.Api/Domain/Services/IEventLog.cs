// -----------------------------------------------------------------------
// <copyright file="IEventLog.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using OutreachGenie.Api.Domain.Abstractions;
using OutreachGenie.Api.Domain.Entities;

namespace OutreachGenie.Api.Domain.Services;

/// <summary>
/// Service for event logging and audit trail.
/// </summary>
public interface IEventLog
{
    /// <summary>
    /// Appends an event to the log.
    /// </summary>
    Task Append(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves events for a campaign.
    /// </summary>
    Task<IEnumerable<DomainEvent>> GetEvents(Guid campaignId, CancellationToken cancellationToken = default);
}

