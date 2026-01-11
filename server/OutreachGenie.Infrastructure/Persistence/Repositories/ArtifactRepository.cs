using Microsoft.EntityFrameworkCore;
using OutreachGenie.Application.Interfaces;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;

namespace OutreachGenie.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for artifact persistence operations.
/// </summary>
public sealed class ArtifactRepository : IArtifactRepository
{
    private readonly OutreachGenieDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtifactRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ArtifactRepository(OutreachGenieDbContext context)
    {
        this.context = context;
    }

    /// <inheritdoc/>
    public async Task<Artifact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.context.Artifacts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Artifact?> GetByKeyAsync(Guid campaignId, ArtifactType type, string key, CancellationToken cancellationToken = default)
    {
        return await this.context.Artifacts
            .Where(a => a.CampaignId == campaignId && a.Type == type && a.Key == key)
            .OrderByDescending(a => a.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Artifact>> GetByTypeAsync(Guid campaignId, ArtifactType type, CancellationToken cancellationToken = default)
    {
        return await this.context.Artifacts
            .Where(a => a.CampaignId == campaignId && a.Type == type)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Artifact>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        return await this.context.Artifacts
            .Where(a => a.CampaignId == campaignId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Artifact> CreateAsync(Artifact artifact, CancellationToken cancellationToken = default)
    {
        this.context.Artifacts.Add(artifact);
        await this.context.SaveChangesAsync(cancellationToken);
        return artifact;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Artifact artifact, CancellationToken cancellationToken = default)
    {
        this.context.Artifacts.Update(artifact);
        await this.context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var artifact = await this.context.Artifacts.FindAsync(new object[] { id }, cancellationToken);
        if (artifact != null)
        {
            this.context.Artifacts.Remove(artifact);
            await this.context.SaveChangesAsync(cancellationToken);
        }
    }
}
