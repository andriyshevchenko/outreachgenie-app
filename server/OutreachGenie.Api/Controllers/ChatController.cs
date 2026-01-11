// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using Microsoft.AspNetCore.Mvc;
using OutreachGenie.Application.Interfaces;
using OutreachGenie.Application.Services.Llm;
using OutreachGenie.Domain.Entities;

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
    private readonly ILlmProvider llm;
    private readonly ICampaignRepository campaigns;
    private readonly ILogger<ChatController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatController"/> class.
    /// </summary>
    /// <param name="llm">LLM provider for generating responses.</param>
    /// <param name="campaigns">Repository for campaign data access.</param>
    /// <param name="logger">Structured logger.</param>
    public ChatController(
        ILlmProvider llm,
        ICampaignRepository campaigns,
        ILogger<ChatController> logger)
    {
        this.llm = llm;
        this.campaigns = campaigns;
        this.logger = logger;
    }

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
        var campaign = await this.campaigns.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign == null)
        {
            return this.NotFound($"Campaign {request.CampaignId} not found");
        }

        var prompt = $"Campaign: {campaign.Name}\nStatus: {campaign.Status}\nUser: {request.Message}";
        var history = new List<Application.Services.Llm.ChatMessage>();
        var response = await this.llm.GenerateResponseAsync(history, prompt, cancellationToken);

        this.logger.LogInformation("Chat response generated for campaign {CampaignId}", request.CampaignId);

        return this.Ok(new ChatResponse(
            Guid.NewGuid(),
            response,
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
        return this.Ok(new List<ChatMessage>());
    }
}
