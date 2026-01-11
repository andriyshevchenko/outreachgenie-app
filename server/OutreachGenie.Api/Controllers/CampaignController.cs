// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using Microsoft.AspNetCore.Mvc;
using OutreachGenie.Application.Interfaces;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Manages campaigns for LinkedIn outreach.
/// Provides endpoints for CRUD operations and campaign lifecycle management.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class CampaignController(ICampaignRepository campaigns) : ControllerBase
{
    /// <summary>
    /// Creates a new campaign.
    /// </summary>
    /// <param name="request">Campaign creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created campaign.</returns>
    [HttpPost]
    public async Task<ActionResult<Campaign>> Create(
        [FromBody] CreateCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Status = CampaignStatus.Initializing,
            TargetAudience = request.TargetAudience,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Documents",
                "OutreachGenie",
                "campaigns",
                Guid.NewGuid().ToString()),
        };
        await campaigns.CreateAsync(campaign, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = campaign.Id }, campaign);
    }

    /// <summary>
    /// Retrieves a campaign by identifier.
    /// </summary>
    /// <param name="id">Campaign identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Campaign details.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Campaign>> Get(Guid id, CancellationToken cancellationToken)
    {
        var campaign = await campaigns.GetByIdAsync(id, cancellationToken);
        if (campaign == null)
        {
            return NotFound($"Campaign {id} not found");
        }

        return campaign;
    }

    /// <summary>
    /// Lists all campaigns.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of campaigns.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Campaign>>> List(CancellationToken cancellationToken)
    {
        var all = await campaigns.GetAllAsync(cancellationToken);
        return Ok(all);
    }

    /// <summary>
    /// Pauses an active campaign.
    /// </summary>
    /// <param name="id">Campaign identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{id}/pause")]
    public async Task<ActionResult> Pause(Guid id, CancellationToken cancellationToken)
    {
        var campaign = await campaigns.GetByIdAsync(id, cancellationToken);
        if (campaign == null)
        {
            return NotFound($"Campaign {id} not found");
        }

        if (campaign.Status != CampaignStatus.Active)
        {
            return BadRequest("Only active campaigns can be paused");
        }

        campaign.Status = CampaignStatus.Paused;
        campaign.UpdatedAt = DateTime.UtcNow;
        await campaigns.UpdateAsync(campaign, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Resumes a paused campaign.
    /// </summary>
    /// <param name="id">Campaign identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("{id}/resume")]
    public async Task<ActionResult> Resume(Guid id, CancellationToken cancellationToken)
    {
        var campaign = await campaigns.GetByIdAsync(id, cancellationToken);
        if (campaign == null)
        {
            return NotFound($"Campaign {id} not found");
        }

        if (campaign.Status != CampaignStatus.Paused)
        {
            return BadRequest("Only paused campaigns can be resumed");
        }

        campaign.Status = CampaignStatus.Active;
        campaign.UpdatedAt = DateTime.UtcNow;
        await campaigns.UpdateAsync(campaign, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deletes a campaign and all related data.
    /// </summary>
    /// <param name="id">Campaign identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var campaign = await campaigns.GetByIdAsync(id, cancellationToken);
        if (campaign == null)
        {
            return NotFound($"Campaign {id} not found");
        }

        await campaigns.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
