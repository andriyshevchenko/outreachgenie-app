using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OutreachGenie.Api.Data;
using OutreachGenie.Api.Domain.Abstractions;
using OutreachGenie.Api.Domain.Entities;

namespace OutreachGenie.Api.Domain.Services;

/// <summary>
/// Event log service implementation.
/// </summary>
public sealed class EventLog : IEventLog
{
    private readonly IDbContextFactory<OutreachGenieDbContext> _contextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventLog"/> class.
    /// </summary>
    public EventLog(IDbContextFactory<OutreachGenieDbContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        this._contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task Append(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await using OutreachGenieDbContext context = await this._contextFactory.CreateDbContextAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(domainEvent);

        string payload = JsonSerializer.Serialize(domainEvent);

        var eventEntity = new Event(
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
    public async Task<IEnumerable<Event>> GetEvents(Guid campaignId, CancellationToken cancellationToken = default)
    {
        await using OutreachGenieDbContext context = await this._contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Events
            .Where(e => e.CampaignId == campaignId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
