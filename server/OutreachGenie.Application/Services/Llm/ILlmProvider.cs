// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using OutreachGenie.Application.Services.Mcp;

namespace OutreachGenie.Application.Services.Llm;

/// <summary>
/// Provides access to Large Language Model capabilities for generating action proposals.
/// Abstracts different LLM providers (OpenAI, Anthropic, local models) behind a common interface.
/// Enforces structured output using the ActionProposal schema for deterministic agent decisions.
/// LLM receives available MCP tools and proposes tool calls dynamically without hardcoded logic.
/// </summary>
/// <example>
/// var provider = new OpenAiProvider(apiKey);
/// var state = await controller.ReloadStateAsync(campaignId);
/// var tools = await mcpRegistry.GetAvailableToolsAsync();
/// var proposal = await provider.GenerateProposalAsync(state, tools, systemPrompt);
/// var validation = DeterministicController.ValidateProposal(state, proposal).
/// </example>
public interface ILlmProvider
{
    /// <summary>
    /// Gets the name of this LLM provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Generates an action proposal based on the current campaign state and available MCP tools.
    /// LLM decides which tools to use and how to use them to accomplish the current task.
    /// </summary>
    /// <param name="state">Complete campaign state snapshot.</param>
    /// <param name="availableTools">List of MCP tools available for execution.</param>
    /// <param name="prompt">System prompt guiding the LLM's decision making.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Structured action proposal with tool name and parameters.</returns>
    Task<ActionProposal> GenerateProposalAsync(
        CampaignState state,
        IReadOnlyList<McpTool> availableTools,
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a natural language response for chat narration.
    /// </summary>
    /// <param name="history">Previous messages in the conversation.</param>
    /// <param name="prompt">User message or system instruction.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Natural language response for user communication.</returns>
    Task<string> GenerateResponseAsync(
        IReadOnlyList<ChatMessage> history,
        string prompt,
        CancellationToken cancellationToken = default);
}
