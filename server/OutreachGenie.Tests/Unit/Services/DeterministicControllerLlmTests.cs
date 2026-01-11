// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json;
using FluentAssertions;
using Moq;
using OutreachGenie.Application.Interfaces;
using OutreachGenie.Application.Services;
using OutreachGenie.Application.Services.Llm;
using OutreachGenie.Application.Services.Mcp;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;
using Xunit;
using TaskStatus = OutreachGenie.Domain.Enums.TaskStatus;

namespace OutreachGenie.Tests.Unit.Services;

/// <summary>
/// Unit tests for DeterministicController LLM orchestration.
/// Tests ExecuteTaskWithLlmAsync method with various scenarios including success, retries, and failures.
/// </summary>
public sealed class DeterministicControllerLlmTests
{
    private readonly Mock<ICampaignRepository> campaigns;
    private readonly Mock<ITaskRepository> tasks;
    private readonly Mock<IArtifactRepository> artifacts;
    private readonly Mock<ILeadRepository> leads;
    private readonly Mock<ILlmProvider> llm;
    private readonly Mock<IMcpToolRegistry> registry;
    private readonly Mock<IMcpServer> server;
    private readonly DeterministicController controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicControllerLlmTests"/> class.
    /// </summary>
    public DeterministicControllerLlmTests()
    {
        this.campaigns = new Mock<ICampaignRepository>();
        this.tasks = new Mock<ITaskRepository>();
        this.artifacts = new Mock<IArtifactRepository>();
        this.leads = new Mock<ILeadRepository>();
        this.llm = new Mock<ILlmProvider>();
        this.registry = new Mock<IMcpToolRegistry>();
        this.server = new Mock<IMcpServer>();
        this.controller = new DeterministicController(
            this.campaigns.Object,
            this.tasks.Object,
            this.artifacts.Object,
            this.leads.Object,
            this.llm.Object,
            this.registry.Object);
    }

    /// <summary>
    /// Tests that ExecuteTaskWithLlmAsync completes task when LLM proposes task_complete.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteTaskWithLlmAsync_ShouldCompleteTaskWhenLlmProposesCompletion()
    {
        var taskId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var task = new CampaignTask
        {
            Id = taskId,
            CampaignId = campaignId,
            Status = TaskStatus.Pending,
            Description = "Test task",
            Type = "Arbitrary",
            CreatedAt = DateTime.UtcNow,
        };
        var campaign = new Campaign { Id = campaignId, Status = CampaignStatus.Active };
        this.tasks.Setup(r => r.GetByIdAsync(taskId, default)).ReturnsAsync(task);
        this.campaigns.Setup(r => r.GetWithAllRelatedAsync(campaignId, default)).ReturnsAsync(campaign);
        this.tasks.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<CampaignTask> { task });
        this.artifacts.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<Artifact>());
        this.leads.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<Lead>());
        this.tasks.Setup(r => r.UpdateAsync(task, default)).Returns(Task.CompletedTask);
        this.artifacts.Setup(r => r.CreateAsync(It.IsAny<Artifact>(), default)).ReturnsAsync((Artifact a, CancellationToken _) => a);
        this.registry.Setup(r => r.DiscoverToolsAsync(default)).ReturnsAsync(new List<McpTool>());
        this.llm.Setup(l => l.GenerateProposalAsync(
            It.IsAny<CampaignState>(),
            It.IsAny<IReadOnlyList<McpTool>>(),
            It.IsAny<string>(),
            default)).ReturnsAsync(new ActionProposal
        {
            TaskId = taskId,
            ActionType = "task_complete",
            Parameters = "{\"result\":\"success\"}",
        });

        await this.controller.ExecuteTaskWithLlmAsync(taskId);

        task.Status.Should().Be(TaskStatus.Done);
        task.CompletedAt.Should().NotBeNull();
        this.llm.Verify(l => l.GenerateProposalAsync(It.IsAny<CampaignState>(), It.IsAny<IReadOnlyList<McpTool>>(), It.IsAny<string>(), default), Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteTaskWithLlmAsync executes tool and continues iteration.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteTaskWithLlmAsync_ShouldExecuteToolAndContinueIteration()
    {
        var taskId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var task = new CampaignTask
        {
            Id = taskId,
            CampaignId = campaignId,
            Status = TaskStatus.Pending,
            Description = "Search LinkedIn",
            Type = "SearchProspects",
            CreatedAt = DateTime.UtcNow,
        };
        var campaign = new Campaign { Id = campaignId, Status = CampaignStatus.Active };
        var tool = new McpTool(
            "browser_navigate",
            "Navigate to URL",
            JsonDocument.Parse("{\"url\":\"string\"}"));
        this.tasks.Setup(r => r.GetByIdAsync(taskId, default)).ReturnsAsync(task);
        this.campaigns.Setup(r => r.GetWithAllRelatedAsync(campaignId, default)).ReturnsAsync(campaign);
        this.tasks.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<CampaignTask> { task });
        this.artifacts.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<Artifact>());
        this.leads.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<Lead>());
        this.tasks.Setup(r => r.UpdateAsync(task, default)).Returns(Task.CompletedTask);
        this.artifacts.Setup(r => r.CreateAsync(It.IsAny<Artifact>(), default)).ReturnsAsync((Artifact a, CancellationToken _) => a);
        this.registry.Setup(r => r.DiscoverToolsAsync(default)).ReturnsAsync(new List<McpTool> { tool });
        this.registry.Setup(r => r.FindToolAsync("browser_navigate", default)).ReturnsAsync(tool);
        this.registry.Setup(r => r.All()).Returns(new List<IMcpServer> { this.server.Object });
        this.server.Setup(s => s.ListToolsAsync(default)).ReturnsAsync(new List<McpTool> { tool });
        this.server.Setup(s => s.CallToolAsync("browser_navigate", It.IsAny<JsonDocument>(), default)).ReturnsAsync(
            JsonDocument.Parse("{\"status\":\"success\",\"message\":\"Navigated to LinkedIn\"}"));
        var callCount = 0;
        this.llm.Setup(l => l.GenerateProposalAsync(
            It.IsAny<CampaignState>(),
            It.IsAny<IReadOnlyList<McpTool>>(),
            It.IsAny<string>(),
            default)).ReturnsAsync(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                return new ActionProposal
                {
                    TaskId = taskId,
                    ActionType = "browser_navigate",
                    Parameters = "{\"url\":\"https://linkedin.com\"}",
                };
            }

            return new ActionProposal
            {
                TaskId = taskId,
                ActionType = "task_complete",
                Parameters = "{\"result\":\"done\"}",
            };
        });

        await this.controller.ExecuteTaskWithLlmAsync(taskId);

        task.Status.Should().Be(TaskStatus.Done);
        this.llm.Verify(l => l.GenerateProposalAsync(It.IsAny<CampaignState>(), It.IsAny<IReadOnlyList<McpTool>>(), It.IsAny<string>(), default), Times.Exactly(2));
        this.server.Verify(s => s.CallToolAsync("browser_navigate", It.IsAny<JsonDocument>(), default), Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteTaskWithLlmAsync handles invalid tool proposals gracefully.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteTaskWithLlmAsync_ShouldHandleInvalidToolGracefully()
    {
        var taskId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var task = new CampaignTask
        {
            Id = taskId,
            CampaignId = campaignId,
            Status = TaskStatus.Pending,
            Description = "Test task",
            Type = "Arbitrary",
            CreatedAt = DateTime.UtcNow,
        };
        var campaign = new Campaign { Id = campaignId, Status = CampaignStatus.Active };
        this.tasks.Setup(r => r.GetByIdAsync(taskId, default)).ReturnsAsync(task);
        this.campaigns.Setup(r => r.GetWithAllRelatedAsync(campaignId, default)).ReturnsAsync(campaign);
        this.tasks.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<CampaignTask> { task });
        this.artifacts.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<Artifact>());
        this.leads.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<Lead>());
        this.tasks.Setup(r => r.UpdateAsync(task, default)).Returns(Task.CompletedTask);
        this.artifacts.Setup(r => r.CreateAsync(It.IsAny<Artifact>(), default)).ReturnsAsync((Artifact a, CancellationToken _) => a);
        this.registry.Setup(r => r.DiscoverToolsAsync(default)).ReturnsAsync(new List<McpTool>());
        this.registry.Setup(r => r.FindToolAsync("nonexistent_tool", default)).ReturnsAsync((McpTool?)null);
        var callCount = 0;
        this.llm.Setup(l => l.GenerateProposalAsync(
            It.IsAny<CampaignState>(),
            It.IsAny<IReadOnlyList<McpTool>>(),
            It.IsAny<string>(),
            default)).ReturnsAsync(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                return new ActionProposal
                {
                    TaskId = taskId,
                    ActionType = "nonexistent_tool",
                    Parameters = "{}",
                };
            }

            return new ActionProposal
            {
                TaskId = taskId,
                ActionType = "task_complete",
                Parameters = "{}",
            };
        });

        await this.controller.ExecuteTaskWithLlmAsync(taskId);

        task.Status.Should().Be(TaskStatus.Done);
        this.artifacts.Verify(a => a.CreateAsync(It.Is<Artifact>(art => art.ContentJson.Contains("invalid_tool")), default), Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteTaskWithLlmAsync retries LLM calls on transient failures.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteTaskWithLlmAsync_ShouldRetryLlmCallsOnFailure()
    {
        var taskId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var task = new CampaignTask
        {
            Id = taskId,
            CampaignId = campaignId,
            Status = TaskStatus.Pending,
            Description = "Test task",
            Type = "Arbitrary",
            CreatedAt = DateTime.UtcNow,
        };
        var campaign = new Campaign { Id = campaignId, Status = CampaignStatus.Active };
        this.tasks.Setup(r => r.GetByIdAsync(taskId, default)).ReturnsAsync(task);
        this.campaigns.Setup(r => r.GetWithAllRelatedAsync(campaignId, default)).ReturnsAsync(campaign);
        this.tasks.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<CampaignTask> { task });
        this.artifacts.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<Artifact>());
        this.leads.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<Lead>());
        this.tasks.Setup(r => r.UpdateAsync(task, default)).Returns(Task.CompletedTask);
        this.artifacts.Setup(r => r.CreateAsync(It.IsAny<Artifact>(), default)).ReturnsAsync((Artifact a, CancellationToken _) => a);
        this.registry.Setup(r => r.DiscoverToolsAsync(default)).ReturnsAsync(new List<McpTool>());
        var attemptCount = 0;
        this.llm.Setup(l => l.GenerateProposalAsync(
            It.IsAny<CampaignState>(),
            It.IsAny<IReadOnlyList<McpTool>>(),
            It.IsAny<string>(),
            default)).Returns<CampaignState, IReadOnlyList<McpTool>, string, CancellationToken>((_, _, _, _) =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new HttpRequestException("Network timeout");
            }

            ActionProposal? proposal = new ActionProposal
            {
                TaskId = taskId,
                ActionType = "task_complete",
                Parameters = "{}",
            };
            return Task.FromResult(proposal);
        });

        await this.controller.ExecuteTaskWithLlmAsync(taskId);

        task.Status.Should().Be(TaskStatus.Done);
        attemptCount.Should().Be(3);
    }

    /// <summary>
    /// Tests that ExecuteTaskWithLlmAsync fails after max consecutive errors.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteTaskWithLlmAsync_ShouldFailAfterMaxConsecutiveErrors()
    {
        var taskId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var task = new CampaignTask
        {
            Id = taskId,
            CampaignId = campaignId,
            Status = TaskStatus.Pending,
            Description = "Test task",
            Type = "Arbitrary",
            RetryCount = 3,
            MaxRetries = 3,
            CreatedAt = DateTime.UtcNow,
        };
        var campaign = new Campaign { Id = campaignId, Status = CampaignStatus.Active };
        this.tasks.Setup(r => r.GetByIdAsync(taskId, default)).ReturnsAsync(task);
        this.campaigns.Setup(r => r.GetWithAllRelatedAsync(campaignId, default)).ReturnsAsync(campaign);
        this.tasks.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<CampaignTask> { task });
        this.artifacts.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<Artifact>());
        this.leads.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<Lead>());
        this.tasks.Setup(r => r.UpdateAsync(task, default)).Returns(Task.CompletedTask);
        this.artifacts.Setup(r => r.CreateAsync(It.IsAny<Artifact>(), default)).ReturnsAsync((Artifact a, CancellationToken _) => a);
        this.registry.Setup(r => r.DiscoverToolsAsync(default)).ReturnsAsync(new List<McpTool>());
        this.llm.Setup(l => l.GenerateProposalAsync(
            It.IsAny<CampaignState>(),
            It.IsAny<IReadOnlyList<McpTool>>(),
            It.IsAny<string>(),
            default)).ThrowsAsync(new InvalidOperationException("LLM service unavailable"));

        var act = async () => await this.controller.ExecuteTaskWithLlmAsync(taskId);

        await act.Should().ThrowAsync<InvalidOperationException>();
        task.Status.Should().BeOneOf(TaskStatus.Failed, TaskStatus.Retrying);
        task.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Tests that ExecuteTaskWithLlmAsync validates JSON parameters before execution.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteTaskWithLlmAsync_ShouldValidateJsonParameters()
    {
        var taskId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var task = new CampaignTask
        {
            Id = taskId,
            CampaignId = campaignId,
            Status = TaskStatus.Pending,
            Description = "Test task",
            Type = "Arbitrary",
            CreatedAt = DateTime.UtcNow,
        };
        var campaign = new Campaign { Id = campaignId, Status = CampaignStatus.Active };
        var tool = new McpTool(
            "test_tool",
            "Test tool",
            JsonDocument.Parse("{\"prop\":\"string\"}"));
        this.tasks.Setup(r => r.GetByIdAsync(taskId, default)).ReturnsAsync(task);
        this.campaigns.Setup(r => r.GetWithAllRelatedAsync(campaignId, default)).ReturnsAsync(campaign);
        this.tasks.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<CampaignTask> { task });
        this.artifacts.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<Artifact>());
        this.leads.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<Lead>());
        this.tasks.Setup(r => r.UpdateAsync(task, default)).Returns(Task.CompletedTask);
        this.artifacts.Setup(r => r.CreateAsync(It.IsAny<Artifact>(), default)).ReturnsAsync((Artifact a, CancellationToken _) => a);
        this.registry.Setup(r => r.DiscoverToolsAsync(default)).ReturnsAsync(new List<McpTool> { tool });
        this.registry.Setup(r => r.FindToolAsync("test_tool", default)).ReturnsAsync(tool);
        var callCount = 0;
        this.llm.Setup(l => l.GenerateProposalAsync(
            It.IsAny<CampaignState>(),
            It.IsAny<IReadOnlyList<McpTool>>(),
            It.IsAny<string>(),
            default)).ReturnsAsync(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                return new ActionProposal
                {
                    TaskId = taskId,
                    ActionType = "test_tool",
                    Parameters = "invalid json {{{",
                };
            }

            return new ActionProposal
            {
                TaskId = taskId,
                ActionType = "task_complete",
                Parameters = "{}",
            };
        });

        await this.controller.ExecuteTaskWithLlmAsync(taskId);

        task.Status.Should().Be(TaskStatus.Done);
        this.artifacts.Verify(a => a.CreateAsync(It.Is<Artifact>(art => art.ContentJson.Contains("invalid_parameters")), default), Times.Once);
    }
}
