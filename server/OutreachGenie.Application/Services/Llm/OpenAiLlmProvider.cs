// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OutreachGenie.Application.Services.Mcp;

namespace OutreachGenie.Application.Services.Llm;

/// <summary>
/// OpenAI provider for generating action proposals using available MCP tools.
/// LLM receives tool schemas and decides dynamically which tools to call and how.
/// No hardcoded logic - LLM drives all automation decisions.
/// </summary>
public sealed class OpenAiLlmProvider : ILlmProvider
{
    [SuppressMessage("SonarLint", "S1075", Justification = "OpenAI API endpoint is a legitimate external URI constant")]
    private const string ApiBaseUrl = "https://api.openai.com/v1/";
    private readonly LlmConfiguration configuration;
    private readonly ILogger<OpenAiLlmProvider> logger;
    private readonly HttpClient httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiLlmProvider"/> class.
    /// </summary>
    /// <param name="apiKey">OpenAI API key.</param>
    /// <param name="configuration">LLM configuration for model, temperature, etc.</param>
    /// <param name="logger">Logger for LLM operations.</param>
    /// <param name="httpClient">HTTP client for API calls.</param>
    public OpenAiLlmProvider(
        string apiKey,
        LlmConfiguration configuration,
        ILogger<OpenAiLlmProvider> logger,
        HttpClient? httpClient = null)
    {
        this.configuration = configuration;
        this.logger = logger;
        this.httpClient = httpClient ?? new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
        this.httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    /// <inheritdoc/>
    public string Name => "OpenAI";

    /// <inheritdoc/>
    public async Task<ActionProposal> GenerateProposalAsync(
        CampaignState state,
        IReadOnlyList<McpTool> availableTools,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Generating action proposal for campaign {CampaignId}", state.Campaign.Id);

        var systemPrompt = BuildSystemPrompt(state, availableTools, prompt);

        var requestBody = new
        {
            model = this.configuration.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = "Based on the current state and available tools, what action should we take next? Respond with a JSON ActionProposal." },
            },
            temperature = this.configuration.Temperature,
            max_tokens = this.configuration.MaxTokens,
            response_format = new { type = "json_object" },
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await this.httpClient.PostAsync("chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseDoc = JsonDocument.Parse(responseJson);

        var messageContent = responseDoc
            .RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrEmpty(messageContent))
        {
            throw new InvalidOperationException("LLM returned empty response");
        }

        var proposal = JsonSerializer.Deserialize<ActionProposal>(messageContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new NullableGuidConverter(),
                new ParametersConverter(),
            },
        });
        if (proposal == null)
        {
            throw new InvalidOperationException("Failed to deserialize ActionProposal from LLM response");
        }

        this.logger.LogInformation(
            "LLM proposed action: {ActionType} for task {TaskId}",
            proposal.ActionType,
            proposal.TaskId);

        return proposal;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateResponseAsync(
        IReadOnlyList<ChatMessage> history,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var messages = history
            .Select(m => new { role = m.Role, content = m.Content })
            .Append(new { role = "user", content = prompt })
            .ToArray();

        var requestBody = new
        {
            model = this.configuration.Model,
            messages,
            temperature = this.configuration.Temperature,
            max_tokens = this.configuration.MaxTokens,
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await this.httpClient.PostAsync("chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseDoc = JsonDocument.Parse(responseJson);

        var messageContent = responseDoc
            .RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return messageContent ?? string.Empty;
    }

    private static string BuildSystemPrompt(CampaignState state, IReadOnlyList<McpTool> availableTools, string basePrompt)
    {
        var sb = new StringBuilder();
        sb.AppendLine(basePrompt);
        sb.AppendLine();
        sb.AppendLine("## Current Campaign State");
        sb.AppendLine($"Campaign: {state.Campaign.Name}");
        sb.AppendLine($"Status: {state.Campaign.Status}");
        sb.AppendLine($"Target Audience: {state.Campaign.TargetAudience}");
        sb.AppendLine($"Tasks: {state.Tasks.Count} total");
        sb.AppendLine($"Artifacts: {state.Artifacts.Count} stored");
        sb.AppendLine($"Leads: {state.Leads.Count} discovered");
        sb.AppendLine();

        var nextTask = state.Tasks
            .Where(t => t.Status == Domain.Enums.TaskStatus.Pending)
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefault();

        if (nextTask != null)
        {
            sb.AppendLine("## Next Task");
            sb.AppendLine($"ID: {nextTask.Id}");
            sb.AppendLine($"Description: {nextTask.Description}");
            sb.AppendLine($"Type: {nextTask.Type}");
            if (!string.IsNullOrEmpty(nextTask.InputJson))
            {
                sb.AppendLine($"Input: {nextTask.InputJson}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Available MCP Tools");
        sb.AppendLine("You can use these tools to accomplish tasks. Choose tools dynamically based on what you need to do:");
        sb.AppendLine();

        foreach (var tool in availableTools)
        {
            sb.AppendLine($"### {tool.Name}");
            sb.AppendLine($"Description: {tool.Description}");
            if (tool.Schema != null)
            {
                sb.AppendLine($"Input Schema: {JsonSerializer.Serialize(tool.Schema)}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("## ActionProposal Schema");
        sb.AppendLine("Respond with JSON matching this structure:");
        sb.AppendLine("{");
        sb.AppendLine("  \"ActionType\": \"<tool_name>\",");
        sb.AppendLine("  \"TaskId\": \"<guid>\",");
        sb.AppendLine("  \"Parameters\": \"<json_string_matching_tool_schema>\"");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
