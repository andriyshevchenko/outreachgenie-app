using OutreachGenie.Api.Domain.Abstractions;
using OutreachGenie.Api.Domain.Entities;

namespace OutreachGenie.Api.Domain.Services;

/// <summary>
/// Service for task management and enforcement.
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Gets the next required task for a campaign.
    /// </summary>
    Task<CampaignTask?> NextRequiredTask(Guid campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new task for a campaign.
    /// </summary>
    Task<Result<CampaignTask>> CreateTask(
        Guid campaignId,
        string title,
        string description,
        bool requiresApproval,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a task as completed.
    /// </summary>
    Task<Result<CampaignTask>> CompleteTask(
        Guid taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets incomplete tasks for a campaign phase.
    /// </summary>
    Task<IEnumerable<CampaignTask>> GetIncompleteTasks(
        Guid campaignId,
        CampaignPhase phase,
        CancellationToken cancellationToken = default);
}
