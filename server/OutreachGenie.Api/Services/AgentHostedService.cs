using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OutreachGenie.Api.Configuration;
using OutreachGenie.Application.Interfaces;
using OutreachGenie.Application.Services;
using OutreachGenie.Domain.Enums;
using TaskStatus = OutreachGenie.Domain.Enums.TaskStatus;

namespace OutreachGenie.Api.Services;

/// <summary>
/// Background service that continuously polls for active campaigns and processes them.
/// Implements "state-driven, not conversation-driven" architecture.
/// </summary>
public sealed class AgentHostedService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly AgentConfiguration configuration;
    private readonly ILogger<AgentHostedService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentHostedService"/> class.
    /// </summary>
    /// <param name="scopeFactory">Service scope factory for creating scoped dependencies.</param>
    /// <param name="configuration">Agent configuration settings.</param>
    /// <param name="logger">Logger instance.</param>
    public AgentHostedService(
        IServiceScopeFactory scopeFactory,
        AgentConfiguration configuration,
        ILogger<AgentHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.configuration = configuration;
        this.logger = logger;
    }

    /// <summary>
    /// Executes the background service loop.
    /// Polls for active campaigns and processes them via DeterministicController.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token for graceful shutdown.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogInformation(
            "Agent background service starting. Polling interval: {IntervalMs}ms, Max concurrent campaigns: {MaxConcurrent}",
            this.configuration.PollingIntervalMs,
            this.configuration.MaxConcurrentCampaigns);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessActiveCampaignsAsync(stoppingToken);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogInformation(ex, "Agent background service cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Unhandled exception in agent background service loop");
            }

            await Task.Delay(configuration.PollingIntervalMs, stoppingToken);
        }

        this.logger.LogInformation("Agent background service stopped");
    }

    private async Task ProcessActiveCampaignsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var campaignRepo = scope.ServiceProvider.GetRequiredService<ICampaignRepository>();
        var taskRepo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
        var controller = scope.ServiceProvider.GetRequiredService<DeterministicController>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IAgentNotificationService>();

        var activeCampaigns = await campaignRepo.GetAllAsync(cancellationToken);
        var campaignsToProcess = activeCampaigns
            .Where(c => c.Status == CampaignStatus.Active)
            .Take(configuration.MaxConcurrentCampaigns)
            .ToList();

        if (campaignsToProcess.Count == 0)
        {
            return;
        }

        logger.LogInformation(
            "Processing {Count} active campaign(s)",
            campaignsToProcess.Count);

        foreach (var campaign in campaignsToProcess)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessCampaignAsync(
                campaign.Id,
                taskRepo,
                controller,
                notificationService,
                cancellationToken);
        }
    }

    private async Task ProcessCampaignAsync(
        Guid campaignId,
        ITaskRepository taskRepo,
        DeterministicController controller,
        IAgentNotificationService notificationService,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Processing campaign {CampaignId}",
                campaignId);

            var pendingTasks = await taskRepo.GetByStatusAsync(
                campaignId,
                TaskStatus.Pending,
                cancellationToken);

            if (pendingTasks.Count == 0)
            {
                logger.LogInformation(
                    "No pending tasks for campaign {CampaignId}",
                    campaignId);
                return;
            }

            var nextTask = pendingTasks
                .OrderBy(t => t.CreatedAt)
                .First();

            logger.LogInformation(
                "Executing task {TaskId} for campaign {CampaignId}",
                nextTask.Id,
                campaignId);

            await controller.ExecuteTaskWithLlmAsync(nextTask.Id, cancellationToken);

            var updatedTask = await taskRepo.GetByIdAsync(nextTask.Id, cancellationToken);

            if (updatedTask != null)
            {
                await notificationService.NotifyTaskStatusChanged(
                    updatedTask.Id.ToString(),
                    updatedTask.Status.ToString());

                logger.LogInformation(
                    "Task {TaskId} completed with status {Status}",
                    updatedTask.Id,
                    updatedTask.Status);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error processing campaign {CampaignId}",
                campaignId);
        }
    }
}
