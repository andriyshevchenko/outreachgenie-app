// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using Microsoft.AspNetCore.Mvc;

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Manages chat interactions with the agent.
/// Provides non-authoritative endpoints for message exchange.
/// NOTE: Chat history is for narration only, not state management.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class ChatController : ControllerBase
{
    /// <summary>
    /// Sends a message to the agent.
    /// </summary>
    /// <param name="request">Message content and campaign context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agent response message.</returns>
    [HttpPost("send")]
    public async Task<ActionResult<ChatResponse>> SendMessage(
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        return Ok(new ChatResponse(
            Guid.NewGuid(),
            "Agent response placeholder",
            DateTime.UtcNow));
    }

    /// <summary>
    /// Retrieves chat history for a campaign.
    /// </summary>
    /// <param name="campaignId">Campaign identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of chat messages.</returns>
    [HttpGet("history/{campaignId}")]
    public async Task<ActionResult<List<ChatMessage>>> GetHistory(
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        return Ok(new List<ChatMessage>());
    }
}
