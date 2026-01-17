// -----------------------------------------------------------------------
// <copyright file="CampaignRepository.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using OutreachGenie.Api.Data;
using OutreachGenie.Api.Domain.Entities;

namespace OutreachGenie.Api.Infrastructure.Repositories;

/// <summary>
/// Campaign repository implementation using DbContext factory pattern.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated public classes", Justification = "Instantiated via dependency injection")]
public sealed class CampaignRepository : ICampaignRepository
{
    private readonly IDbContextFactory<OutreachGenieDbContext> contextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CampaignRepository"/> class.
    /// </summary>
    public CampaignRepository(IDbContextFactory<OutreachGenieDbContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        this.contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<Campaign?> LoadWithTasks(Guid campaignId, CancellationToken cancellationToken = default)
    {
        await using OutreachGenieDbContext context = await this.contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Campaigns
            .Include(c => c.Tasks.OrderBy(t => t.OrderIndex))
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Campaign?> LoadComplete(Guid campaignId, CancellationToken cancellationToken = default)
    {
        await using OutreachGenieDbContext context = await this.contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Campaigns
            .Include(c => c.Tasks.OrderBy(t => t.OrderIndex))
            .Include(c => c.Leads)
            .Include(c => c.Artifacts)
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Campaign?> FindById(Guid id, CancellationToken cancellationToken = default)
    {
        await using OutreachGenieDbContext context = await this.contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Campaigns.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Campaign>> GetAll(CancellationToken cancellationToken = default)
    {
        await using OutreachGenieDbContext context = await this.contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Campaigns.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task Add(Campaign entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await using OutreachGenieDbContext context = await this.contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Campaigns.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task Update(Campaign entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await using OutreachGenieDbContext context = await this.contextFactory.CreateDbContextAsync(cancellationToken);

        // Attach the campaign
        context.Campaigns.Attach(entity);

        // Find new tasks and add them
        var existingTaskIds = context.ChangeTracker.Entries<CampaignTask>()
            .Select(e => e.Entity.Id)
            .ToHashSet();

        IEnumerable<CampaignTask> newTasks = entity.Tasks.Where(task => !existingTaskIds.Contains(task.Id));
        foreach (CampaignTask task in newTasks)
        {
            context.CampaignTasks.Add(task);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task Remove(Campaign entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await using OutreachGenieDbContext context = await this.contextFactory.CreateDbContextAsync(cancellationToken);
        context.Campaigns.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> SaveChanges(CancellationToken cancellationToken = default)
    {
        // Note: With factory pattern, SaveChanges is called within each operation
        // This method is kept for interface compatibility but creates a new context
        await using OutreachGenieDbContext context = await this.contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.SaveChangesAsync(cancellationToken);
    }
}

