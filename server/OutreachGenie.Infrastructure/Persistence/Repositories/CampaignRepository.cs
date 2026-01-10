using Microsoft.EntityFrameworkCore;
using OutreachGenie.Application.Interfaces;
using OutreachGenie.Domain.Entities;

namespace OutreachGenie.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for campaign persistence operations.
/// </summary>
public sealed class CampaignRepository : ICampaignRepository
{
    private readonly OutreachGenieDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CampaignRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public CampaignRepository(OutreachGenieDbContext context)
    {
        this.context = context;
    }

    /// <inheritdoc/>
    public async Task<Campaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.context.Campaigns
            .Include(c => c.Tasks)
            .Include(c => c.Artifacts)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Campaign?> GetWithAllRelatedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.context.Campaigns
            .Include(c => c.Tasks)
            .Include(c => c.Artifacts)
            .Include(c => c.Leads)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Campaign>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await this.context.Campaigns
            .Include(c => c.Tasks)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Campaign> CreateAsync(Campaign campaign, CancellationToken cancellationToken = default)
    {
        this.context.Campaigns.Add(campaign);
        await this.context.SaveChangesAsync(cancellationToken);
        return campaign;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Campaign campaign, CancellationToken cancellationToken = default)
    {
        campaign.UpdatedAt = DateTime.UtcNow;
        this.context.Campaigns.Update(campaign);
        await this.context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var campaign = await this.context.Campaigns.FindAsync(new object[] { id }, cancellationToken);
        if (campaign != null)
        {
            this.context.Campaigns.Remove(campaign);
            await this.context.SaveChangesAsync(cancellationToken);
        }
    }
}
