// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using OutreachGenie.Application.Services;
using OutreachGenie.Application.Services.Llm;
using OutreachGenie.Application.Services.Mcp;

namespace OutreachGenie.Tests.Integration.Fakes;

/// <summary>
/// Fake LLM provider for testing without real API calls.
/// Returns predictable responses for deterministic testing.
/// </summary>
internal sealed class FakeLlmProvider : ILlmProvider
{
    /// <summary>
    /// Gets the name of the LLM provider.
    /// </summary>
    public string Name => "FakeLlm";

    /// <summary>
    /// Generates a fake action proposal for testing.
    /// </summary>
    /// <param name="state">The campaign state.</param>
    /// <param name="availableTools">Available MCP tools.</param>
    /// <param name="prompt">The prompt for generation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task complete action proposal.</returns>
    public Task<ActionProposal> GenerateProposalAsync(
        CampaignState state,
        IReadOnlyList<McpTool> availableTools,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ActionProposal
        {
            ActionType = "task_complete",
            Parameters = "{}",
        });
    }

    /// <summary>
    /// Generates a fake response for testing.
    /// </summary>
    /// <param name="history">Chat history.</param>
    /// <param name="prompt">The prompt for generation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A fake response string.</returns>
    public Task<string> GenerateResponseAsync(
        IReadOnlyList<ChatMessage> history,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Fake response");
    }
}
