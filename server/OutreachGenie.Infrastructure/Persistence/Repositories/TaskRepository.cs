using Microsoft.EntityFrameworkCore;
using OutreachGenie.Application.Interfaces;
using OutreachGenie.Domain.Entities;

namespace OutreachGenie.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for task persistence operations.
/// </summary>
public sealed class TaskRepository : ITaskRepository
{
    private readonly OutreachGenieDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public TaskRepository(OutreachGenieDbContext context)
    {
        this.context = context;
    }

    /// <inheritdoc/>
    public async Task<CampaignTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<CampaignTask>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        return await this.context.Tasks
            .Where(t => t.CampaignId == campaignId)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<CampaignTask>> GetByStatusAsync(Guid campaignId, OutreachGenie.Domain.Enums.TaskStatus status, CancellationToken cancellationToken = default)
    {
        return await this.context.Tasks
            .Where(t => t.CampaignId == campaignId && t.Status == status)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CampaignTask> CreateAsync(CampaignTask task, CancellationToken cancellationToken = default)
    {
        this.context.Tasks.Add(task);
        await this.context.SaveChangesAsync(cancellationToken);
        return task;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(CampaignTask task, CancellationToken cancellationToken = default)
    {
        this.context.Tasks.Update(task);
        await this.context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await this.context.Tasks.FindAsync(new object[] { id }, cancellationToken);
        if (task != null)
        {
            this.context.Tasks.Remove(task);
            await this.context.SaveChangesAsync(cancellationToken);
        }
    }
}
