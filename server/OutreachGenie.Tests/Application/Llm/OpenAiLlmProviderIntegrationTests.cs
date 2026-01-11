// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OutreachGenie.Application.Services;
using OutreachGenie.Application.Services.Llm;
using OutreachGenie.Application.Services.Mcp;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;
using Xunit;

namespace OutreachGenie.Tests.Application.Llm;

#pragma warning disable SA1600
#pragma warning disable CS1591

/// <summary>
/// Integration tests for OpenAiLlmProvider with real OpenAI API.
/// Requires OPENAI_API_KEY environment variable to be set.
/// Tests are skipped if API key is not available.
/// </summary>
public sealed class OpenAiLlmProviderIntegrationTests
{
    private readonly string? apiKey;

    public OpenAiLlmProviderIntegrationTests()
    {
        this.apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    [Fact]
    public async Task GenerateProposalAsync_ShouldReturnValidActionProposal()
    {
        if (string.IsNullOrEmpty(this.apiKey))
        {
            return;
        }

        var config = new LlmConfiguration(
            temperature: 0.5,
            tokens: 500,
            model: "gpt-4o-mini",
            retries: 3,
            timeout: 30);
        var provider = new OpenAiLlmProvider(
            this.apiKey,
            config,
            NullLogger<OpenAiLlmProvider>.Instance);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Test Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "Software engineers",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var task = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Description = "Navigate to example.com website",
            Type = "NavigateWebsite",
            Status = Domain.Enums.TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        var state = new CampaignState(campaign, new List<CampaignTask> { task }, [], []);
        var tools = new List<McpTool>
        {
            new McpTool(
                "browser_navigate",
                "Navigate to a URL in the browser",
                JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"url\":{\"type\":\"string\"}},\"required\":[\"url\"]}")),
            new McpTool(
                "task_complete",
                "Mark the task as complete",
                JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"result\":{\"type\":\"string\"}},\"required\":[\"result\"]}")),
        };
        var prompt = "Execute the pending task: Navigate to example.com website. Use browser_navigate tool to open the URL, then mark as complete.";

        var proposal = await provider.GenerateProposalAsync(state, tools, prompt);

        proposal.Should().NotBeNull();
        proposal.ActionType.Should().NotBeNullOrEmpty();
        proposal.ActionType.Should().BeOneOf("browser_navigate", "task_complete");
        if (proposal.ActionType == "browser_navigate")
        {
            proposal.Parameters.Should().NotBeNullOrEmpty();
            var parameters = JsonDocument.Parse(proposal.Parameters!);
            parameters.RootElement.TryGetProperty("url", out var urlElement).Should().BeTrue();
            urlElement.GetString().Should().Contain("example.com");
        }
    }

    [Fact]
    public async Task GenerateProposalAsync_ShouldChooseAppropriateToolBasedOnContext()
    {
        if (string.IsNullOrEmpty(this.apiKey))
        {
            return;
        }

        var config = new LlmConfiguration(
            temperature: 0.7,
            tokens: 500,
            model: "gpt-4o-mini",
            retries: 3,
            timeout: 30);
        var provider = new OpenAiLlmProvider(
            this.apiKey,
            config,
            NullLogger<OpenAiLlmProvider>.Instance);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Research Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "Technical decision makers",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var task = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Description = "Save research results to file",
            Type = "SaveResults",
            Status = Domain.Enums.TaskStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
        };
        var artifact = new Artifact
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Type = ArtifactType.Context,
            Key = "research_data",
            ContentJson = "{\"findings\":[\"Item 1\",\"Item 2\"]}",
            Source = ArtifactSource.Agent,
            CreatedAt = DateTime.UtcNow,
        };
        var state = new CampaignState(campaign, new List<CampaignTask> { task }, new List<Artifact> { artifact }, []);
        var tools = new List<McpTool>
        {
            new McpTool(
                "write_file",
                "Write content to a file",
                JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"path\":{\"type\":\"string\"},\"content\":{\"type\":\"string\"}},\"required\":[\"path\",\"content\"]}")),
            new McpTool(
                "browser_navigate",
                "Navigate to a URL",
                JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"url\":{\"type\":\"string\"}},\"required\":[\"url\"]}")),
            new McpTool(
                "task_complete",
                "Mark task as complete",
                JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"result\":{\"type\":\"string\"}},\"required\":[\"result\"]}")),
        };
        var prompt = "The research data has been collected in the artifact. Save it to a file named 'results.json' and mark the task complete.";

        var proposal = await provider.GenerateProposalAsync(state, tools, prompt);

        proposal.Should().NotBeNull();
        proposal.ActionType.Should().NotBeNullOrEmpty();
        proposal.ActionType.Should().BeOneOf("write_file", "task_complete");
        if (proposal.ActionType == "write_file")
        {
            proposal.Parameters.Should().NotBeNullOrEmpty();
            var parameters = JsonDocument.Parse(proposal.Parameters!);
            parameters.RootElement.TryGetProperty("path", out var pathElement).Should().BeTrue();
            pathElement.GetString().Should().Contain("results");
        }
    }

    [Fact]
    public async Task GenerateProposalAsync_ShouldHandleErrorAndProposeRecovery()
    {
        if (string.IsNullOrEmpty(this.apiKey))
        {
            return;
        }

        var config = new LlmConfiguration(
            temperature: 0.7,
            tokens: 500,
            model: "gpt-4o-mini",
            retries: 3,
            timeout: 30);
        var provider = new OpenAiLlmProvider(
            this.apiKey,
            config,
            NullLogger<OpenAiLlmProvider>.Instance);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Web Scraping Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "Data analysts",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var task = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Description = "Extract data from website",
            Type = "DataExtraction",
            Status = Domain.Enums.TaskStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
        };
        var state = new CampaignState(campaign, new List<CampaignTask> { task }, [], []);
        var tools = new List<McpTool>
        {
            new McpTool(
                "browser_navigate",
                "Navigate to a URL",
                JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"url\":{\"type\":\"string\"}},\"required\":[\"url\"]}")),
            new McpTool(
                "browser_evaluate",
                "Execute JavaScript on the page",
                JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"function\":{\"type\":\"string\"}},\"required\":[\"function\"]}")),
        };
        var prompt = "Previous action: browser_navigate. Result: {\"error\":\"Page not found (404)\"}. The page doesn't exist. Try a different approach or handle the error.";

        var proposal = await provider.GenerateProposalAsync(state, tools, prompt);

        proposal.Should().NotBeNull();
        proposal.ActionType.Should().NotBeNullOrEmpty();
        proposal.Should().Match<ActionProposal>(p =>
            p.ActionType == "browser_navigate" ||
            p.ActionType == "browser_evaluate" ||
            p.ActionType == "task_complete");
    }

    [Fact]
    public async Task GenerateProposalAsync_ShouldReturnStructuredJsonResponse()
    {
        if (string.IsNullOrEmpty(this.apiKey))
        {
            return;
        }

        var config = new LlmConfiguration(
            temperature: 0.7,
            tokens: 300,
            model: "gpt-4o-mini",
            retries: 3,
            timeout: 30);
        var provider = new OpenAiLlmProvider(
            this.apiKey,
            config,
            NullLogger<OpenAiLlmProvider>.Instance);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Simple Task Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "General audience",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var taskId = Guid.NewGuid();
        var task = new CampaignTask
        {
            Id = taskId,
            CampaignId = campaign.Id,
            Description = "Complete a simple action",
            Type = "SimpleAction",
            Status = Domain.Enums.TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        var state = new CampaignState(campaign, new List<CampaignTask> { task }, [], []);
        var tools = new List<McpTool>
        {
            new McpTool(
                "task_complete",
                "Mark the task as complete with result",
                JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"result\":{\"type\":\"string\"}},\"required\":[\"result\"]}")),
        };
        var prompt = "Complete this simple task immediately by calling task_complete.";

        var proposal = await provider.GenerateProposalAsync(state, tools, prompt);

        proposal.Should().NotBeNull();
        proposal.ActionType.Should().Be("task_complete");
        proposal.TaskId.Should().NotBeNull();
        proposal.Parameters.Should().NotBeNullOrEmpty();
        var parseAction = () => JsonDocument.Parse(proposal.Parameters!);
        parseAction.Should().NotThrow("Parameters should be valid JSON");
    }

    [Fact]
    public async Task GenerateResponseAsync_ShouldReturnHumanReadableNarration()
    {
        if (string.IsNullOrEmpty(this.apiKey))
        {
            return;
        }

        var config = new LlmConfiguration(
            temperature: 0.5,
            tokens: 200,
            model: "gpt-4o-mini",
            retries: 3,
            timeout: 30);
        var provider = new OpenAiLlmProvider(
            this.apiKey,
            config,
            NullLogger<OpenAiLlmProvider>.Instance);
        var history = new List<ChatMessage>();
        var prompt = "Explain what we are doing: Searching LinkedIn for CTOs in San Francisco to build a prospect list for outreach.";

        var response = await provider.GenerateResponseAsync(history, prompt);

        response.Should().NotBeNullOrEmpty();
        response.Should().Contain("LinkedIn");
        response.Should().MatchRegex("(?i)(cto|search|prospect|outreach)");
    }
}
