// -----------------------------------------------------------------------
// <copyright file="TaskService.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using OutreachGenie.Api.Domain.Abstractions;
using OutreachGenie.Api.Domain.Entities;
using OutreachGenie.Api.Domain.Models;
using OutreachGenie.Api.Infrastructure.Repositories;

namespace OutreachGenie.Api.Domain.Services;

/// <summary>
/// Task service implementation with enforcement logic.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated public classes", Justification = "Instantiated via dependency injection")]
public sealed class TaskService : ITaskService
{
    private readonly ICampaignRepository campaignRepository;
    private readonly IEventLog eventLog;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskService"/> class.
    /// </summary>
    public TaskService(
        ICampaignRepository campaignRepository,
        IEventLog eventLog)
    {
        this.campaignRepository = campaignRepository;
        this.eventLog = eventLog;
    }

    /// <inheritdoc />
    public async Task<CampaignTask?> NextRequiredTask(
        Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        Campaign? campaign = await this.campaignRepository.LoadWithTasks(campaignId, cancellationToken);

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
        bool requiresPreviousTask = true,
        CancellationToken cancellationToken = default)
    {
        Campaign? campaign = await this.campaignRepository.LoadWithTasks(campaignId, cancellationToken);

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
            requiresPreviousTask,
            DateTime.UtcNow);

        campaign.Tasks.Add(task);
        await this.campaignRepository.Update(campaign, cancellationToken);

        // Log event
        TaskCreatedEvent taskEvent = new(task.Id, campaignId, title, orderIndex);
        await this.eventLog.Append(taskEvent, cancellationToken);

        return Result<CampaignTask>.Success(task);
    }

    /// <inheritdoc />
    public async Task<Result<CampaignTask>> CompleteTask(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        // Find the task by scanning all campaigns with tasks
        // This is inefficient but acceptable for MVP - in production we'd have a task repository
        var allCampaigns = await this.campaignRepository.GetAll(cancellationToken);

        Campaign? campaign = null;
        CampaignTask? task = null;

        foreach (var camp in allCampaigns)
        {
            var loadedCampaign = await this.campaignRepository.LoadWithTasks(camp.Id, cancellationToken);
            if (loadedCampaign != null)
            {
                task = loadedCampaign.Tasks.FirstOrDefault(t => t.Id == taskId);
                if (task != null)
                {
                    campaign = loadedCampaign;
                    break;
                }
            }
        }

        if (task == null || campaign == null)
        {
            return Result<CampaignTask>.Failure("Task not found");
        }

        // ENFORCEMENT: If task requires previous task, check that all previous tasks are completed
        if (task.RequiresPreviousTask && task.OrderIndex > 0)
        {
            var previousTasks = campaign.Tasks
                .Where(t => t.OrderIndex < task.OrderIndex)
                .OrderBy(t => t.OrderIndex)
                .ToList();

            var incompleteTask = previousTasks.FirstOrDefault(t => t.Status != Domain.Entities.TaskStatus.Completed);
            if (incompleteTask != null)
            {
                return Result<CampaignTask>.Failure(
                    $"Cannot complete task '{task.Title}'. Previous task '{incompleteTask.Title}' (index {incompleteTask.OrderIndex}) must be completed first. " +
                    $"Current task order: {task.OrderIndex}. This ensures campaign steps are not skipped.");
            }
        }

        // Mark as completed
        task.CompletedAt = DateTime.UtcNow;
        task.Status = Domain.Entities.TaskStatus.Completed;

        await this.campaignRepository.Update(campaign, cancellationToken);

        // Log event
        var taskCompletedEvent = new TaskCompletedEvent(task.Id, campaign.Id, task.Title);
        await this.eventLog.Append(taskCompletedEvent, cancellationToken);

        return Result<CampaignTask>.Success(task);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CampaignTask>> GetIncompleteTasks(
        Guid campaignId,
        CampaignPhase phase,
        CancellationToken cancellationToken = default)
    {
        Campaign? campaign = await this.campaignRepository.LoadWithTasks(campaignId, cancellationToken);

        if (campaign == null || campaign.Phase != phase)
        {
            return Enumerable.Empty<CampaignTask>();
        }

        return campaign.Tasks.Where(t => t.Status != Domain.Entities.TaskStatus.Completed).ToList();
    }
}

