// -----------------------------------------------------------------------
// <copyright file="TaskDto.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using OutreachGenie.Api.Domain.Entities;

namespace OutreachGenie.Api.Models;

/// <summary>
/// Task data transfer object.
/// </summary>
public sealed class TaskDto
{
    /// <summary>
    /// Task identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Campaign identifier.
    /// </summary>
    public Guid CampaignId { get; set; }

    /// <summary>
    /// Task title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Task description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Task status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Order index.
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// Requires approval flag.
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Completion timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Creates DTO from entity.
    /// </summary>
    internal static TaskDto FromEntity(CampaignTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        return new TaskDto
        {
            Id = task.Id,
            CampaignId = task.CampaignId,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            OrderIndex = task.OrderIndex,
            RequiresApproval = task.RequiresApproval,
            CreatedAt = task.CreatedAt,
            CompletedAt = task.CompletedAt,
        };
    }
}

