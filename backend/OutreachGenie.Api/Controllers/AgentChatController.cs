// -----------------------------------------------------------------------
// <copyright file="AgentChatController.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Controller for agent chat interactions that bypasses MapAGUI bug.
/// This properly merges agent tools with client tools.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AgentChatController : ControllerBase
{
    private readonly IChatClient chatClient;
    private readonly IReadOnlyList<AITool> tools;
    private readonly string systemPrompt;
    private readonly ILogger<AgentChatController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentChatController"/> class.
    /// </summary>
    public AgentChatController(
        IChatClient chatClient,
        IReadOnlyList<AITool> tools,
        string systemPrompt,
        ILogger<AgentChatController> logger)
    {
        this.chatClient = chatClient;
        this.tools = tools;
        this.systemPrompt = systemPrompt;
        this.logger = logger;
    }

    /// <summary>
    /// Streams chat responses from the agent with proper tool support.
    /// </summary>
    /// <param name="request">The chat request containing messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server-Sent Events stream of chat responses.</returns>
    [HttpPost("stream")]
    public async Task StreamChat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        this.logger.LogInformation("Starting chat stream with {MessageCount} messages", request.Messages.Count);

        // Convert simple messages to proper ChatMessage objects
        // Add system prompt at the beginning
        List<ChatMessage> chatMessages =
        [
            new ChatMessage(ChatRole.System, this.systemPrompt),
            .. request.Messages.Select(msg => new ChatMessage(
                role: msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? ChatRole.User : ChatRole.Assistant,
                contents: [new TextContent(msg.Content)]))
        ];

        this.logger.LogInformation("Processing {MessageCount} messages with {ToolCount} tools", chatMessages.Count, this.tools.Count);

        // Log EXACTLY what we're sending
        foreach (var msg in chatMessages)
        {
            this.logger.LogInformation("Message - Role: {Role}, Content: {Content}",
                msg.Role, msg.Text);
        }

        this.logger.LogInformation("ToolMode: {ToolMode}, Tools: [{Tools}]",
            "RequireAny",
            string.Join(", ", this.tools.Select(t => t.Name)));

        // Set response headers for Server-Sent Events
        this.Response.Headers.Append("Content-Type", "text/event-stream");
        this.Response.Headers.Append("Cache-Control", "no-cache");
        this.Response.Headers.Append("Connection", "keep-alive");

        try
        {
            // Send start event
            await this.WriteSseEvent("start", new { status = "running" }, cancellationToken);

            // Call IChatClient directly with tools
            // UseFunctionInvocation middleware handles function calling automatically
            ChatOptions options = new()
            {
                Tools = this.tools.ToList(), // Convert IReadOnlyList to IList
                ToolMode = ChatToolMode.Auto, // Let the LLM decide when to call tools
            };

            await foreach (ChatResponseUpdate update in this.chatClient.GetStreamingResponseAsync(
                chatMessages,
                options,
                cancellationToken))
            {
                // Convert streaming update to our SSE format
                object sseData = new
                {
                    AuthorName = update.AuthorName ?? "OutreachGenieAgent",
                    Role = update.Role?.ToString(),
                    Contents = update.Text != null ? new[] { new { type = "text", Text = update.Text } } : Array.Empty<object>(),
                    MessageId = update.MessageId,
                    ResponseId = update.ResponseId,
                };

                await this.WriteSseEvent("update", sseData, cancellationToken);
            }

            // Send completion event
            await this.WriteSseEvent("done", new { status = "completed" }, cancellationToken);
            this.logger.LogInformation("Chat stream completed successfully");
        }
        catch (OperationCanceledException ex)
        {
            this.logger.LogInformation(ex, "Chat stream cancelled");
            await this.WriteSseEvent("done", new { status = "cancelled" }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            this.logger.LogError(ex, "Error during chat stream");
            await this.WriteSseEvent("error", new { message = ex.Message }, cancellationToken);
        }
    }

    private async Task WriteSseEvent(string eventType, object data, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(data);
        await this.Response.WriteAsync($"event: {eventType}\n", cancellationToken);
        await this.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await this.Response.Body.FlushAsync(cancellationToken);
    }
}

