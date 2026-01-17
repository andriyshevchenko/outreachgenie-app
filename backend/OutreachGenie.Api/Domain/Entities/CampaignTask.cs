// -----------------------------------------------------------------------
// <copyright file="CampaignTask.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OutreachGenie.Api.Domain.Entities;

/// <summary>
/// Represents a task within a campaign.
/// </summary>
public sealed class CampaignTask
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CampaignTask"/> class.
    /// </summary>
#pragma warning disable S107 // Methods should not have too many parameters
    public CampaignTask(
        Guid id,
        Guid campaignId,
        string title,
        string description,
        TaskStatus status,
        int orderIndex,
        bool requiresApproval,
        bool requiresPreviousTask,
        DateTime createdAt)
#pragma warning restore S107 // Methods should not have too many parameters
    {
        this.Id = id;
        this.CampaignId = campaignId;
        this.Title = title;
        this.Description = description;
        this.Status = status;
        this.OrderIndex = orderIndex;
        this.RequiresApproval = requiresApproval;
        this.RequiresPreviousTask = requiresPreviousTask;
        this.CreatedAt = createdAt;
    }

    /// <summary>
    /// Task identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Campaign identifier.
    /// </summary>
    public Guid CampaignId { get; private set; }

    /// <summary>
    /// Task title.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Task description.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Current status.
    /// </summary>
    public TaskStatus Status { get; set; }

    /// <summary>
    /// Execution order index.
    /// </summary>
    public int OrderIndex { get; private set; }

    /// <summary>
    /// Indicates whether approval is required.
    /// </summary>
    public bool RequiresApproval { get; private set; }

    /// <summary>
    /// Indicates whether the previous task must be completed before this task can be started.
    /// Enforces sequential execution to prevent the LLM from skipping steps.
    /// </summary>
    public bool RequiresPreviousTask { get; private set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Completion timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Parent campaign.
    /// </summary>
    public Campaign Campaign { get; private set; } = null!;

    private CampaignTask()
    {
        // Required for EF Core
    }
}

