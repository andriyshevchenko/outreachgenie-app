using Microsoft.EntityFrameworkCore;
using OutreachGenie.Api.Domain.Abstractions;
using OutreachGenie.Api.Domain.Entities;
using OutreachGenie.Api.Domain.Models;
using OutreachGenie.Api.Infrastructure.Repositories;

namespace OutreachGenie.Api.Domain.Services;

/// <summary>
/// Task service implementation with enforcement logic.
/// </summary>
public sealed class TaskService : ITaskService
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly IEventLog _eventLog;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskService"/> class.
    /// </summary>
    public TaskService(
        ICampaignRepository campaignRepository,
        IEventLog eventLog)
    {
        this._campaignRepository = campaignRepository;
        this._eventLog = eventLog;
    }

    /// <inheritdoc />
    public async Task<CampaignTask?> NextRequiredTask(
        Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        Campaign? campaign = await this._campaignRepository.LoadWithTasks(campaignId, cancellationToken);

        if (campaign == null)
        {
            return null;
        }

        return campaign.Tasks
            .Where(t => t.Status != Domain.Entities.TaskStatus.Completed)
            .OrderBy(t => t.OrderIndex)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Result<CampaignTask>> CreateTask(
        Guid campaignId,
        string title,
        string description,
        bool requiresApproval,
        CancellationToken cancellationToken = default)
    {
        Campaign? campaign = await this._campaignRepository.LoadWithTasks(campaignId, cancellationToken);

        if (campaign == null)
        {
            return Result<CampaignTask>.Failure($"Campaign {campaignId} not found");
        }

        int orderIndex = campaign.Tasks.Any() ? campaign.Tasks.Max(t => t.OrderIndex) + 1 : 0;

        CampaignTask task = new(
            Guid.NewGuid(),
            campaignId,
            title,
            description,
            Domain.Entities.TaskStatus.Pending,
            orderIndex,
            requiresApproval,
            DateTime.UtcNow);

        campaign.Tasks.Add(task);
        await this._campaignRepository.Update(campaign, cancellationToken);

        // Log event
        TaskCreatedEvent taskEvent = new(task.Id, campaignId, title, orderIndex);
        await this._eventLog.Append(taskEvent, cancellationToken);

        return Result<CampaignTask>.Success(task);
    }

    /// <inheritdoc />
    public async Task<Result<CampaignTask>> CompleteTask(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        // This is a simplified implementation - in production we'd need a task repository
        // For now, this demonstrates the pattern
        await Task.CompletedTask;

        return Result<CampaignTask>.Failure("Task not found");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CampaignTask>> GetIncompleteTasks(
        Guid campaignId,
        CampaignPhase phase,
        CancellationToken cancellationToken = default)
    {
        Campaign? campaign = await this._campaignRepository.LoadWithTasks(campaignId, cancellationToken);

        if (campaign == null || campaign.Phase != phase)
        {
            return Enumerable.Empty<CampaignTask>();
        }

        return campaign.Tasks.Where(t => t.Status != Domain.Entities.TaskStatus.Completed).ToList();
    }
}
