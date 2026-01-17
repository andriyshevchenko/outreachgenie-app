// -----------------------------------------------------------------------
// <copyright file="EventLog.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OutreachGenie.Api.Data;
using OutreachGenie.Api.Domain.Abstractions;
using OutreachGenie.Api.Domain.Entities;

namespace OutreachGenie.Api.Domain.Services;

/// <summary>
/// Event log service implementation.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated public classes", Justification = "Instantiated via dependency injection")]
public sealed class EventLog : IEventLog
{
    private readonly IDbContextFactory<OutreachGenieDbContext> contextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventLog"/> class.
    /// </summary>
    public EventLog(IDbContextFactory<OutreachGenieDbContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        this.contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task Append(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await using OutreachGenieDbContext context = await this.contextFactory.CreateDbContextAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(domainEvent);

        string payload = JsonSerializer.Serialize(domainEvent);

        var eventEntity = new DomainEvent(
            domainEvent.EventId,
            domainEvent.EventType,
            null, // Will be set from the actual event if it contains CampaignId
            domainEvent.Timestamp,
            EventActor.Agent,
            payload);

        await context.Events.AddAsync(eventEntity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DomainEvent>> GetEvents(Guid campaignId, CancellationToken cancellationToken = default)
    {
        await using OutreachGenieDbContext context = await this.contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Events
            .Where(e => e.CampaignId == campaignId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(cancellationToken);
    }
}

