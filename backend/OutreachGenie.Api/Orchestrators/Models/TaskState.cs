// -----------------------------------------------------------------------
// <copyright file="TaskState.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace OutreachGenie.Api.Orchestrators.Models;

/// <summary>
/// Represents a task in the campaign state.
/// </summary>
public sealed class TaskState
{
    /// <summary>
    /// Task identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Task title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Task description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Task status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Order index.
    /// </summary>
    [JsonPropertyName("orderIndex")]
    public int OrderIndex { get; set; }
}

