// -----------------------------------------------------------------------
// <copyright file="CampaignState.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace OutreachGenie.Api.Orchestrators.Models;

/// <summary>
/// Represents the state of a campaign for Agent Framework state management.
/// This state is synchronized between database and AG-UI protocol.
/// </summary>
public sealed class CampaignState
{
    /// <summary>
    /// Campaign identifier.
    /// </summary>
    [JsonPropertyName("campaignId")]
    public Guid CampaignId { get; set; }

    /// <summary>
    /// Campaign name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current phase of the campaign.
    /// </summary>
    [JsonPropertyName("phase")]
    public string Phase { get; set; } = string.Empty;

    /// <summary>
    /// Number of completed tasks.
    /// </summary>
    [JsonPropertyName("completedTasks")]
    public int CompletedTasks { get; set; }

    /// <summary>
    /// Total number of tasks.
    /// </summary>
    [JsonPropertyName("totalTasks")]
    public int TotalTasks { get; set; }

    /// <summary>
    /// Current pending task.
    /// </summary>
    [JsonPropertyName("currentTask")]
    public TaskState? CurrentTask { get; set; }

    /// <summary>
    /// Number of leads discovered.
    /// </summary>
    [JsonPropertyName("leadsDiscovered")]
    public int LeadsDiscovered { get; set; }

    /// <summary>
    /// Number of leads scored.
    /// </summary>
    [JsonPropertyName("leadsScored")]
    public int LeadsScored { get; set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }
}

