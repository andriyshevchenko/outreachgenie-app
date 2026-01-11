// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OutreachGenie.Application.Interfaces;
using OutreachGenie.Application.Services;
using OutreachGenie.Application.Services.Llm;
using OutreachGenie.Application.Services.Mcp;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;
using OutreachGenie.Infrastructure.Persistence;
using OutreachGenie.Infrastructure.Persistence.Repositories;
using OutreachGenie.Tests.Integration.Fakes;
using OutreachGenie.Tests.Integration.Fixtures;
using Xunit;
using TaskStatus = OutreachGenie.Domain.Enums.TaskStatus;

namespace OutreachGenie.Tests.Integration.Services;

/// <summary>
/// Integration tests for campaign resume and recovery logic.
/// Validates that campaigns can be paused, application restarted, and resumed without context loss.
/// Tests the core requirement: state persistence allows full recovery after restart.
/// </summary>
[Collection("Database")]
public sealed class CampaignResumeIntegrationTests
{
    private readonly DatabaseFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="CampaignResumeIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The database fixture.</param>
    public CampaignResumeIntegrationTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Tests full campaign resume scenario:
    /// 1. Create campaign with tasks
    /// 2. Execute some tasks, store artifacts
    /// 3. Simulate restart (dispose controller, create new one)
    /// 4. Resume campaign
    /// 5. Verify state recovery and continuation from last completed task.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ResumeCampaign_ShouldContinueFromLastCompletedTask()
    {
        await using var context = this.fixture.CreateDbContext();
        var controller = CreateController(context);
        var campaignId = Guid.NewGuid();
        var campaign = new Campaign
        {
            Id = campaignId,
            Name = "Resume Test Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "CTOs in San Francisco",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/resume",
        };
        var task1 = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Description = "Initialize campaign context",
            Type = "Initialize",
            Status = TaskStatus.Done,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            StartedAt = DateTime.UtcNow.AddMinutes(-9),
            CompletedAt = DateTime.UtcNow.AddMinutes(-8),
            OutputJson = "{\"status\":\"initialized\"}",
        };
        var task2 = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Description = "Search LinkedIn for prospects",
            Type = "SearchProspects",
            Status = TaskStatus.Done,
            CreatedAt = DateTime.UtcNow.AddMinutes(-8),
            StartedAt = DateTime.UtcNow.AddMinutes(-7),
            CompletedAt = DateTime.UtcNow.AddMinutes(-5),
            OutputJson = "{\"prospects_found\":25}",
        };
        var task3 = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Description = "Score and rank leads",
            Type = "ScoreLeads",
            Status = TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
        };
        var artifact1 = new Artifact
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Type = ArtifactType.Context,
            Key = "campaign_context",
            ContentJson = "{\"target\":\"CTOs\",\"location\":\"San Francisco\"}",
            Source = ArtifactSource.User,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
        };
        var artifact2 = new Artifact
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Type = ArtifactType.Leads,
            Key = "prospects_list",
            ContentJson = "[{\"name\":\"John Doe\",\"title\":\"CTO\"}]",
            Source = ArtifactSource.Agent,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
        };
        context.Campaigns.Add(campaign);
        context.Tasks.AddRange(task1, task2, task3);
        context.Artifacts.AddRange(artifact1, artifact2);
        await context.SaveChangesAsync();

        var stateBeforeRestart = await controller.ReloadStateAsync(campaignId);

        stateBeforeRestart.Campaign.Id.Should().Be(campaignId);
        stateBeforeRestart.Tasks.Should().HaveCount(3);
        stateBeforeRestart.Tasks.Count(t => t.Status == TaskStatus.Done).Should().Be(2);
        stateBeforeRestart.Tasks.Count(t => t.Status == TaskStatus.Pending).Should().Be(1);
        stateBeforeRestart.Artifacts.Should().HaveCount(2);

        var nextTaskBeforeRestart = DeterministicController.SelectNextTask(stateBeforeRestart);
        nextTaskBeforeRestart.Should().NotBeNull();
        nextTaskBeforeRestart!.Id.Should().Be(task3.Id);
        nextTaskBeforeRestart.Description.Should().Be("Score and rank leads");

        await using var context2 = this.fixture.CreateDbContext();
        var newController = CreateController(context2);

        var stateAfterRestart = await newController.ReloadStateAsync(campaignId);

        stateAfterRestart.Campaign.Id.Should().Be(campaignId);
        stateAfterRestart.Tasks.Should().HaveCount(3);
        stateAfterRestart.Tasks.Count(t => t.Status == TaskStatus.Done).Should().Be(2);
        stateAfterRestart.Tasks.Count(t => t.Status == TaskStatus.Pending).Should().Be(1);
        stateAfterRestart.Artifacts.Should().HaveCount(2);

        var nextTaskAfterRestart = DeterministicController.SelectNextTask(stateAfterRestart);
        nextTaskAfterRestart.Should().NotBeNull();
        nextTaskAfterRestart!.Id.Should().Be(task3.Id);
        nextTaskAfterRestart.Description.Should().Be("Score and rank leads");

        var completedTasks = stateAfterRestart.Tasks.Where(t => t.Status == TaskStatus.Done).ToList();
        completedTasks.Should().HaveCount(2);
        completedTasks.Should().Contain(t => t.Id == task1.Id);
        completedTasks.Should().Contain(t => t.Id == task2.Id);
        completedTasks.All(t => t.CompletedAt.HasValue).Should().BeTrue();
        completedTasks.All(t => !string.IsNullOrEmpty(t.OutputJson)).Should().BeTrue();

        var contextArtifact = stateAfterRestart.Artifacts.FirstOrDefault(a => a.Key == "campaign_context");
        contextArtifact.Should().NotBeNull();
        contextArtifact!.ContentJson.Should().Contain("CTOs");

        var leadsArtifact = stateAfterRestart.Artifacts.FirstOrDefault(a => a.Key == "prospects_list");
        leadsArtifact.Should().NotBeNull();
        leadsArtifact!.ContentJson.Should().Contain("John Doe");
    }

    /// <summary>
    /// Tests campaign resume with failed tasks requiring retry.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ResumeCampaign_ShouldRetryFailedTasks()
    {
        await using var context = this.fixture.CreateDbContext();
        var controller = CreateController(context);
        var campaignId = Guid.NewGuid();
        var campaign = new Campaign
        {
            Id = campaignId,
            Name = "Retry Test Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "VPs of Engineering",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/retry",
        };
        var task1 = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Description = "Failed task with retries remaining",
            Type = "WebScraping",
            Status = TaskStatus.Retrying,
            RetryCount = 1,
            MaxRetries = 3,
            ErrorMessage = "Network timeout",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            StartedAt = DateTime.UtcNow.AddMinutes(-4),
        };
        var task2 = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Description = "Pending task waiting for retry",
            Type = "DataExtraction",
            Status = TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-3),
        };
        context.Campaigns.Add(campaign);
        context.Tasks.AddRange(task1, task2);
        await context.SaveChangesAsync();

        var state = await controller.ReloadStateAsync(campaignId);

        state.Tasks.Should().HaveCount(2);
        var retryingTask = state.Tasks.First(t => t.Status == TaskStatus.Retrying);
        retryingTask.RetryCount.Should().Be(1);
        retryingTask.MaxRetries.Should().Be(3);
        retryingTask.ErrorMessage.Should().Be("Network timeout");

        var nextTask = DeterministicController.SelectNextTask(state);
        nextTask.Should().NotBeNull("campaign has pending tasks that can execute independently");
        nextTask!.Id.Should().Be(task2.Id);
        nextTask.Status.Should().Be(TaskStatus.Pending);
        nextTask.Description.Should().Be("Pending task waiting for retry");
    }

    /// <summary>
    /// Tests campaign resume after pause and reactivation.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ResumeCampaign_ShouldHandlePauseAndReactivation()
    {
        await using var context = this.fixture.CreateDbContext();
        var controller = CreateController(context);
        var campaignId = Guid.NewGuid();
        var campaign = new Campaign
        {
            Id = campaignId,
            Name = "Pause Test Campaign",
            Status = CampaignStatus.Paused,
            TargetAudience = "Engineering Managers",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/pause",
        };
        var task1 = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Description = "Pending task while paused",
            Type = "SearchProspects",
            Status = TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        context.Campaigns.Add(campaign);
        context.Tasks.Add(task1);
        await context.SaveChangesAsync();

        var statePaused = await controller.ReloadStateAsync(campaignId);
        var nextTaskPaused = DeterministicController.SelectNextTask(statePaused);
        nextTaskPaused.Should().BeNull("no tasks selected when campaign is paused");

        await controller.TransitionCampaignStatusAsync(campaignId, CampaignStatus.Active);

        var stateActive = await controller.ReloadStateAsync(campaignId);
        var nextTaskActive = DeterministicController.SelectNextTask(stateActive);
        nextTaskActive.Should().NotBeNull();
        nextTaskActive!.Id.Should().Be(task1.Id);
        nextTaskActive.Status.Should().Be(TaskStatus.Pending);
    }

    /// <summary>
    /// Tests that state reload preserves artifact versioning and history.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ResumeCampaign_ShouldPreserveArtifactVersioning()
    {
        await using var context = this.fixture.CreateDbContext();
        var controller = CreateController(context);
        var campaignId = Guid.NewGuid();
        var campaign = new Campaign
        {
            Id = campaignId,
            Name = "Artifact Versioning Test",
            Status = CampaignStatus.Active,
            TargetAudience = "Tech Leaders",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/versioning",
        };
        var artifact1 = new Artifact
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Type = ArtifactType.Context,
            Key = "campaign_state",
            ContentJson = "{\"version\":1}",
            Source = ArtifactSource.Agent,
            Version = 1,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
        };
        var artifact2 = new Artifact
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Type = ArtifactType.Context,
            Key = "campaign_state",
            ContentJson = "{\"version\":2}",
            Source = ArtifactSource.Agent,
            Version = 2,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
        };
        context.Campaigns.Add(campaign);
        context.Artifacts.AddRange(artifact1, artifact2);
        await context.SaveChangesAsync();

        var state = await controller.ReloadStateAsync(campaignId);

        var stateArtifacts = state.Artifacts.Where(a => a.Key == "campaign_state").ToList();
        stateArtifacts.Should().HaveCount(2);
        stateArtifacts.Should().Contain(a => a.Version == 1);
        stateArtifacts.Should().Contain(a => a.Version == 2);

        var latestVersion = stateArtifacts.OrderByDescending(a => a.Version).First();
        latestVersion.Version.Should().Be(2);
        latestVersion.ContentJson.Should().Contain("\"version\":2");
    }

    /// <summary>
    /// Creates a controller with fresh dependencies for testing.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <returns>A configured DeterministicController.</returns>
    private static DeterministicController CreateController(OutreachGenieDbContext context)
    {
        var campaignRepo = new CampaignRepository(context);
        var taskRepo = new TaskRepository(context);
        var artifactRepo = new ArtifactRepository(context);
        var leadRepo = new LeadRepository(context);
        var llmProvider = new FakeLlmProvider();
        var mcpRegistry = new FakeMcpToolRegistry();
        return new DeterministicController(
            campaignRepo,
            taskRepo,
            artifactRepo,
            leadRepo,
            llmProvider,
            mcpRegistry);
    }
}
