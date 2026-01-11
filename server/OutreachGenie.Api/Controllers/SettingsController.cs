// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using Microsoft.AspNetCore.Mvc;

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Manages application settings and configuration.
/// Provides endpoints for retrieving and updating system settings.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class SettingsController : ControllerBase
{
    /// <summary>
    /// Retrieves current application settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Settings object.</returns>
    [HttpGet]
    public async Task<ActionResult<Settings>> Get(CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        return Ok(new Settings(
            "openai",
            "gpt-4",
            0.7,
            2000,
            3,
            30));
    }

    /// <summary>
    /// Updates application settings.
    /// </summary>
    /// <param name="request">Updated settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>NoContent.</returns>
    [HttpPut]
    public async Task<ActionResult> Update(
        [FromBody] UpdateSettingsRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        return NoContent();
    }
}
