// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using Microsoft.AspNetCore.Mvc;
using OutreachGenie.Application.Interfaces;
using OutreachGenie.Domain.Entities;
using TaskStatus = OutreachGenie.Domain.Enums.TaskStatus;

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Manages campaign tasks.
/// Provides endpoints for querying task status and retrieving task details.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class TaskController(ITaskRepository tasks) : ControllerBase
{
    /// <summary>
    /// Retrieves a task by identifier.
    /// </summary>
    /// <param name="id">Task identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task details or NotFound.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<CampaignTask>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(id, cancellationToken);
        if (task == null)
        {
            return NotFound();
        }

        return Ok(task);
    }

    /// <summary>
    /// Lists all tasks for a campaign.
    /// </summary>
    /// <param name="campaignId">Campaign identifier.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of tasks.</returns>
    [HttpGet("campaign/{campaignId}")]
    public async Task<ActionResult<List<CampaignTask>>> ListByCampaign(
        Guid campaignId,
        [FromQuery] TaskStatus? status,
        CancellationToken cancellationToken)
    {
        var result = status.HasValue
            ? await tasks.GetByStatusAsync(campaignId, status.Value, cancellationToken)
            : await tasks.GetByCampaignIdAsync(campaignId, cancellationToken);

        return Ok(result);
    }
}
