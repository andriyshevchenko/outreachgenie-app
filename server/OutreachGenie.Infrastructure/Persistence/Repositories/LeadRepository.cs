using Microsoft.EntityFrameworkCore;
using OutreachGenie.Application.Interfaces;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;

namespace OutreachGenie.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for lead persistence operations.
/// </summary>
public sealed class LeadRepository : ILeadRepository
{
    private readonly OutreachGenieDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeadRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public LeadRepository(OutreachGenieDbContext context)
    {
        this.context = context;
    }

    /// <inheritdoc/>
    public async Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.context.Leads
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Lead>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        return await this.context.Leads
            .Where(l => l.CampaignId == campaignId)
            .OrderByDescending(l => l.WeightScore)
            .ThenBy(l => l.FullName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Lead>> GetByStatusAsync(Guid campaignId, LeadStatus status, CancellationToken cancellationToken = default)
    {
        return await this.context.Leads
            .Where(l => l.CampaignId == campaignId && l.Status == status)
            .OrderByDescending(l => l.WeightScore)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Lead>> GetTopLeadsAsync(Guid campaignId, int count, CancellationToken cancellationToken = default)
    {
        return await this.context.Leads
            .Where(l => l.CampaignId == campaignId)
            .OrderByDescending(l => l.WeightScore)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Lead> CreateAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        this.context.Leads.Add(lead);
        await this.context.SaveChangesAsync(cancellationToken);
        return lead;
    }

    /// <inheritdoc/>
    public async Task CreateBatchAsync(IEnumerable<Lead> leads, CancellationToken cancellationToken = default)
    {
        await this.context.Leads.AddRangeAsync(leads, cancellationToken);
        await this.context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        lead.UpdatedAt = DateTime.UtcNow;
        this.context.Leads.Update(lead);
        await this.context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lead = await this.context.Leads.FindAsync(new object[] { id }, cancellationToken);
        if (lead != null)
        {
            this.context.Leads.Remove(lead);
            await this.context.SaveChangesAsync(cancellationToken);
        }
    }
}
