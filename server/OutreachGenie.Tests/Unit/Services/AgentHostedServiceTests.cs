// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OutreachGenie.Api.Configuration;
using OutreachGenie.Api.Services;
using OutreachGenie.Application.Interfaces;
using OutreachGenie.Application.Services;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;
using OutreachGenie.Tests.Integration.Fakes;
using Xunit;
using TaskStatus = OutreachGenie.Domain.Enums.TaskStatus;

namespace OutreachGenie.Tests.Unit.Services;

/// <summary>
/// Unit tests for AgentHostedService background service.
/// Tests polling, campaign processing, error handling, and cancellation.
/// </summary>
public sealed class AgentHostedServiceTests
{
    private readonly Mock<IServiceScopeFactory> scopeFactory;
    private readonly Mock<IServiceScope> scope;
    private readonly Mock<IServiceProvider> serviceProvider;
    private readonly Mock<ICampaignRepository> campaignRepo;
    private readonly Mock<ITaskRepository> taskRepo;
    private readonly FakeDeterministicController controller;
    private readonly Mock<IAgentNotificationService> notificationService;
    private readonly Mock<ILogger<AgentHostedService>> logger;
    private readonly AgentConfiguration configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentHostedServiceTests"/> class.
    /// </summary>
    public AgentHostedServiceTests()
    {
        this.scopeFactory = new Mock<IServiceScopeFactory>();
        this.scope = new Mock<IServiceScope>();
        this.serviceProvider = new Mock<IServiceProvider>();
        this.campaignRepo = new Mock<ICampaignRepository>();
        this.taskRepo = new Mock<ITaskRepository>();
        this.controller = new FakeDeterministicController();
        this.notificationService = new Mock<IAgentNotificationService>();
        this.logger = new Mock<ILogger<AgentHostedService>>();
        this.configuration = new AgentConfiguration
        {
            PollingIntervalMs = 100,
            MaxConcurrentCampaigns = 1,
        };

        this.scopeFactory.Setup(f => f.CreateScope()).Returns(this.scope.Object);
        this.scope.Setup(s => s.ServiceProvider).Returns(this.serviceProvider.Object);
        this.serviceProvider.Setup(p => p.GetService(typeof(ICampaignRepository))).Returns(this.campaignRepo.Object);
        this.serviceProvider.Setup(p => p.GetService(typeof(ITaskRepository))).Returns(this.taskRepo.Object);
        this.serviceProvider.Setup(p => p.GetService(typeof(DeterministicController))).Returns((object)this.controller);
        this.serviceProvider.Setup(p => p.GetService(typeof(IAgentNotificationService))).Returns(this.notificationService.Object);
    }

    /// <summary>
    /// Tests that service starts and logs startup message.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsync_ShouldLogStartupMessage()
    {
        this.campaignRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Campaign>());
        var service = new AgentHostedService(this.scopeFactory.Object, this.configuration, this.logger.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        await service.StartAsync(cts.Token);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);
        this.logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Agent background service starting")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that service processes active campaigns.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProcessActiveCampaignsAsync_ShouldProcessActiveCampaigns()
    {
        var campaignId = Guid.NewGuid();
        var campaign = new Campaign { Id = campaignId, Status = CampaignStatus.Active };
        var taskId = Guid.NewGuid();
        var task = new CampaignTask { Id = taskId, CampaignId = campaignId, Status = TaskStatus.Pending, CreatedAt = DateTime.UtcNow };
        this.campaignRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Campaign> { campaign });
        this.taskRepo.Setup(r => r.GetByStatusAsync(campaignId, TaskStatus.Pending, It.IsAny<CancellationToken>())).ReturnsAsync(new List<CampaignTask> { task });
        this.taskRepo.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        var service = new AgentHostedService(this.scopeFactory.Object, this.configuration, this.logger.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);
        this.controller.ExecutedTasks.Should().Contain(taskId);
    }

    /// <summary>
    /// Tests that service skips campaigns with no pending tasks.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProcessCampaignAsync_ShouldSkipCampaignWithNoPendingTasks()
    {
        var campaignId = Guid.NewGuid();
        var campaign = new Campaign { Id = campaignId, Status = CampaignStatus.Active };
        this.campaignRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Campaign> { campaign });
        this.taskRepo.Setup(r => r.GetByStatusAsync(campaignId, TaskStatus.Pending, It.IsAny<CancellationToken>())).ReturnsAsync(new List<CampaignTask>());
        var service = new AgentHostedService(this.scopeFactory.Object, this.configuration, this.logger.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);
        this.controller.ExecutedTasks.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that service processes oldest pending task first.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProcessCampaignAsync_ShouldProcessOldestPendingTaskFirst()
    {
        var campaignId = Guid.NewGuid();
        var campaign = new Campaign { Id = campaignId, Status = CampaignStatus.Active };
        var oldTaskId = Guid.NewGuid();
        var newTaskId = Guid.NewGuid();
        var oldTask = new CampaignTask { Id = oldTaskId, CampaignId = campaignId, Status = TaskStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
        var newTask = new CampaignTask { Id = newTaskId, CampaignId = campaignId, Status = TaskStatus.Pending, CreatedAt = DateTime.UtcNow };
        this.campaignRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Campaign> { campaign });
        this.taskRepo.Setup(r => r.GetByStatusAsync(campaignId, TaskStatus.Pending, It.IsAny<CancellationToken>())).ReturnsAsync(new List<CampaignTask> { newTask, oldTask });
        this.taskRepo.Setup(r => r.GetByIdAsync(oldTaskId, It.IsAny<CancellationToken>())).ReturnsAsync(oldTask);
        var service = new AgentHostedService(this.scopeFactory.Object, this.configuration, this.logger.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);
        this.controller.ExecutedTasks.Should().Contain(oldTaskId);
    }

    /// <summary>
    /// Tests that service sends notification after task completion.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProcessCampaignAsync_ShouldSendNotificationAfterTaskCompletion()
    {
        var campaignId = Guid.NewGuid();
        var campaign = new Campaign { Id = campaignId, Status = CampaignStatus.Active };
        var taskId = Guid.NewGuid();
        var task = new CampaignTask { Id = taskId, CampaignId = campaignId, Status = TaskStatus.Done, CreatedAt = DateTime.UtcNow };
        this.campaignRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Campaign> { campaign });
        this.taskRepo.Setup(r => r.GetByStatusAsync(campaignId, TaskStatus.Pending, It.IsAny<CancellationToken>())).ReturnsAsync(new List<CampaignTask> { task });
        this.taskRepo.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        var service = new AgentHostedService(this.scopeFactory.Object, this.configuration, this.logger.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);
        this.notificationService.Verify(n => n.NotifyTaskStatusChanged(taskId.ToString(), "Done"), Times.AtLeastOnce);
    }

    /// <summary>
    /// Tests that service respects max concurrent campaigns limit.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProcessActiveCampaignsAsync_ShouldRespectMaxConcurrentCampaignsLimit()
    {
        var campaign1 = new Campaign { Id = Guid.NewGuid(), Status = CampaignStatus.Active };
        var campaign2 = new Campaign { Id = Guid.NewGuid(), Status = CampaignStatus.Active };
        var task1 = new CampaignTask { Id = Guid.NewGuid(), CampaignId = campaign1.Id, Status = TaskStatus.Pending, CreatedAt = DateTime.UtcNow };
        this.campaignRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Campaign> { campaign1, campaign2 });
        this.taskRepo.Setup(r => r.GetByStatusAsync(campaign1.Id, TaskStatus.Pending, It.IsAny<CancellationToken>())).ReturnsAsync(new List<CampaignTask> { task1 });
        this.taskRepo.Setup(r => r.GetByStatusAsync(campaign2.Id, TaskStatus.Pending, It.IsAny<CancellationToken>())).ReturnsAsync(new List<CampaignTask>());
        this.taskRepo.Setup(r => r.GetByIdAsync(task1.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task1);
        var service = new AgentHostedService(this.scopeFactory.Object, this.configuration, this.logger.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);
        this.logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing 1 active campaign(s)")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Tests that service handles controller exceptions gracefully.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProcessCampaignAsync_ShouldHandleControllerExceptionGracefully()
    {
        var campaignId = Guid.NewGuid();
        var campaign = new Campaign { Id = campaignId, Status = CampaignStatus.Active };
        var taskId = Guid.NewGuid();
        var task = new CampaignTask { Id = taskId, CampaignId = campaignId, Status = TaskStatus.Pending, CreatedAt = DateTime.UtcNow };
        this.campaignRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Campaign> { campaign });
        this.taskRepo.Setup(r => r.GetByStatusAsync(campaignId, TaskStatus.Pending, It.IsAny<CancellationToken>())).ReturnsAsync(new List<CampaignTask> { task });
        this.controller.ThrowOnExecute(new InvalidOperationException("Test exception"));
        var service = new AgentHostedService(this.scopeFactory.Object, this.configuration, this.logger.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);
        this.logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error processing campaign")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Tests that service stops gracefully on cancellation.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsync_ShouldStopGracefullyOnCancellation()
    {
        this.campaignRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Campaign>());
        var service = new AgentHostedService(this.scopeFactory.Object, this.configuration, this.logger.Object);
        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await cts.CancelAsync();
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);
        this.logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Agent background service stopped")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that service skips inactive campaigns.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProcessActiveCampaignsAsync_ShouldSkipInactiveCampaigns()
    {
        var activeCampaign = new Campaign { Id = Guid.NewGuid(), Status = CampaignStatus.Active };
        var pausedCampaign = new Campaign { Id = Guid.NewGuid(), Status = CampaignStatus.Paused };
        var completedCampaign = new Campaign { Id = Guid.NewGuid(), Status = CampaignStatus.Completed };
        this.campaignRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Campaign> { activeCampaign, pausedCampaign, completedCampaign });
        this.taskRepo.Setup(r => r.GetByStatusAsync(activeCampaign.Id, TaskStatus.Pending, It.IsAny<CancellationToken>())).ReturnsAsync(new List<CampaignTask>());
        var service = new AgentHostedService(this.scopeFactory.Object, this.configuration, this.logger.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);
        this.taskRepo.Verify(r => r.GetByStatusAsync(pausedCampaign.Id, It.IsAny<TaskStatus>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tests that service handles null updated task gracefully.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProcessCampaignAsync_ShouldHandleNullUpdatedTaskGracefully()
    {
        var campaignId = Guid.NewGuid();
        var campaign = new Campaign { Id = campaignId, Status = CampaignStatus.Active };
        var taskId = Guid.NewGuid();
        var task = new CampaignTask { Id = taskId, CampaignId = campaignId, Status = TaskStatus.Pending, CreatedAt = DateTime.UtcNow };
        this.campaignRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Campaign> { campaign });
        this.taskRepo.Setup(r => r.GetByStatusAsync(campaignId, TaskStatus.Pending, It.IsAny<CancellationToken>())).ReturnsAsync(new List<CampaignTask> { task });
        this.taskRepo.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>())).ReturnsAsync((CampaignTask?)null);
        var service = new AgentHostedService(this.scopeFactory.Object, this.configuration, this.logger.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);
        this.notificationService.Verify(n => n.NotifyTaskStatusChanged(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Tests that service does not process campaigns when none are active.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProcessActiveCampaignsAsync_ShouldNotProcessWhenNoCampaignsExist()
    {
        this.campaignRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Campaign>());
        var service = new AgentHostedService(this.scopeFactory.Object, this.configuration, this.logger.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);
        this.controller.ExecutedTasks.Should().BeEmpty();
    }
}
