// -----------------------------------------------------------------------
// <copyright file="CampaignDto.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using OutreachGenie.Api.Domain.Entities;

namespace OutreachGenie.Api.Models;

/// <summary>
/// Campaign data transfer object.
/// </summary>
public sealed class CampaignDto
{
    /// <summary>
    /// Campaign identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Campaign name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current phase.
    /// </summary>
    public string Phase { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Campaign tasks.
    /// </summary>
    public List<TaskDto> Tasks { get; init; } = [];

    /// <summary>
    /// Creates DTO from entity.
    /// </summary>
    internal static CampaignDto FromEntity(Campaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);

        return new CampaignDto
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Phase = campaign.Phase.ToString(),
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt,
            Tasks = campaign.Tasks.Select(TaskDto.FromEntity).ToList(),
        };
    }
}

