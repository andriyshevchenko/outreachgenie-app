using OutreachGenie.Domain.Entities;
using TaskStatus = OutreachGenie.Domain.Enums.TaskStatus;

namespace OutreachGenie.Application.Interfaces;

/// <summary>
/// Repository interface for task persistence operations.
/// </summary>
public interface ITaskRepository
{
    /// <summary>
    /// Gets a task by identifier.
    /// </summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The task or null if not found.</returns>
    Task<CampaignTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tasks for a campaign.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of tasks.</returns>
    Task<List<CampaignTask>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks by campaign and status.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="status">The task status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of tasks matching criteria.</returns>
    Task<List<CampaignTask>> GetByStatusAsync(Guid campaignId, TaskStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new task.
    /// </summary>
    /// <param name="task">The task to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created task.</returns>
    Task<CampaignTask> CreateAsync(CampaignTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    /// <param name="task">The task to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    Task UpdateAsync(CampaignTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a task.
    /// </summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
