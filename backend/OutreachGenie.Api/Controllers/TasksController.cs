// -----------------------------------------------------------------------
// <copyright file="TasksController.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using OutreachGenie.Api.Domain.Abstractions;
using OutreachGenie.Api.Domain.Entities;
using OutreachGenie.Api.Domain.Services;
using OutreachGenie.Api.Models;

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Controller for campaign task operations.
/// </summary>
[ApiController]
[Route("api/campaigns/{campaignId}/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService taskService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TasksController"/> class.
    /// </summary>
    public TasksController(ITaskService taskService)
    {
        this.taskService = taskService;
    }

    /// <summary>
    /// Creates a new task for a campaign.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTask(
        Guid campaignId,
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest("Request body is required");
        }

        Result<CampaignTask> result = await this.taskService.CreateTask(
            campaignId,
            request.Title,
            request.Description,
            request.RequiresApproval ?? false,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result.ErrorMessage);
        }

        return Ok(TaskDto.FromEntity(result.Value));
    }
}

