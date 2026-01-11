// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using Microsoft.AspNetCore.Mvc;
using OutreachGenie.Application.Interfaces;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Manages campaign artifacts.
/// Provides endpoints for CRUD operations on typed artifacts.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class ArtifactController(IArtifactRepository artifacts) : ControllerBase
{
    /// <summary>
    /// Retrieves an artifact by identifier.
    /// </summary>
    /// <param name="id">Artifact identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Artifact or NotFound.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Artifact>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var artifact = await artifacts.GetByIdAsync(id, cancellationToken);
        if (artifact == null)
        {
            return NotFound();
        }

        return Ok(artifact);
    }

    /// <summary>
    /// Lists artifacts by campaign and optional type filter.
    /// </summary>
    /// <param name="campaignId">Campaign identifier.</param>
    /// <param name="type">Optional artifact type filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of artifacts.</returns>
    [HttpGet("campaign/{campaignId}")]
    public async Task<ActionResult<List<Artifact>>> ListByCampaign(
        Guid campaignId,
        [FromQuery] ArtifactType? type,
        CancellationToken cancellationToken)
    {
        var result = type.HasValue
            ? await artifacts.GetByTypeAsync(campaignId, type.Value, cancellationToken)
            : await artifacts.GetByCampaignIdAsync(campaignId, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Retrieves an artifact by campaign, type, and key.
    /// </summary>
    /// <param name="campaignId">Campaign identifier.</param>
    /// <param name="type">Artifact type.</param>
    /// <param name="key">Artifact key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Artifact or NotFound.</returns>
    [HttpGet("campaign/{campaignId}/type/{type}/key/{key}")]
    public async Task<ActionResult<Artifact>> GetByKey(
        Guid campaignId,
        ArtifactType type,
        string key,
        CancellationToken cancellationToken)
    {
        var artifact = await artifacts.GetByKeyAsync(campaignId, type, key, cancellationToken);
        if (artifact == null)
        {
            return NotFound();
        }

        return Ok(artifact);
    }

    /// <summary>
    /// Creates a new artifact.
    /// </summary>
    /// <param name="request">Artifact creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created artifact.</returns>
    [HttpPost]
    public async Task<ActionResult<Artifact>> Create(
        [FromBody] CreateArtifactRequest request,
        CancellationToken cancellationToken)
    {
        var artifact = new Artifact
        {
            Id = Guid.NewGuid(),
            CampaignId = request.CampaignId,
            Type = request.Type,
            Key = request.Key,
            ContentJson = request.ContentJson,
            Source = request.Source,
            Version = request.Version,
            CreatedAt = DateTime.UtcNow,
        };
        await artifacts.CreateAsync(artifact, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = artifact.Id }, artifact);
    }

    /// <summary>
    /// Updates an existing artifact.
    /// </summary>
    /// <param name="id">Artifact identifier.</param>
    /// <param name="request">Update details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>NoContent or NotFound.</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(
        Guid id,
        [FromBody] UpdateArtifactRequest request,
        CancellationToken cancellationToken)
    {
        var artifact = await artifacts.GetByIdAsync(id, cancellationToken);
        if (artifact == null)
        {
            return NotFound();
        }

        artifact.ContentJson = request.ContentJson;
        artifact.Version = request.Version;
        await artifacts.UpdateAsync(artifact, cancellationToken);
        return NoContent();
    }
}
