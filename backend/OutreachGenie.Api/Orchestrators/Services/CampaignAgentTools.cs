using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OutreachGenie.Api.Domain.Abstractions;
using OutreachGenie.Api.Domain.Entities;
using OutreachGenie.Api.Domain.Models;
using OutreachGenie.Api.Domain.Services;
using OutreachGenie.Api.Infrastructure.Repositories;

namespace OutreachGenie.Api.Orchestrators.Services;

/// <summary>
/// Agent tools for campaign management.
/// These tools are the interface between the LLM and the system.
/// All tools enforce invariants and log actions for auditability.
/// </summary>
public sealed class CampaignAgentTools
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly ICampaignRepository _campaignRepository;
    private readonly ITaskService _taskService;
    private readonly IEventLog _eventLog;
    private readonly ILogger<CampaignAgentTools> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CampaignAgentTools"/> class.
    /// </summary>
    public CampaignAgentTools(
        ICampaignRepository campaignRepository,
        ITaskService taskService,
        IEventLog eventLog,
        ILogger<CampaignAgentTools> logger)
    {
        this._campaignRepository = campaignRepository;
        this._taskService = taskService;
        this._eventLog = eventLog;
        this._logger = logger;
    }

    /// <summary>
    /// Creates a new task for the campaign.
    /// </summary>
    [Description("Create a new task in the campaign. Tasks are executed in order and cannot be skipped.")]
    public async Task<string> CreateTask(
        [Description("Campaign identifier")] Guid campaignId,
        [Description("Task title")] string title,
        [Description("Detailed task description")] string description,
        [Description("Whether this task requires user approval")] bool requiresApproval = false)
    {
        this._logger.LogInformation(
            "Agent creating task: {Title} for campaign {CampaignId}",
            title,
            campaignId);

        Result<CampaignTask> result = await this._taskService.CreateTask(
            campaignId,
            title,
            description,
            requiresApproval);

        if (!result.IsSuccess)
        {
            this._logger.LogError("Failed to create task: {Error}", result.Error);
            return $"Error: {result.Error}";
        }

        return $"Task created successfully: {title} (ID: {result.Value.Id})";
    }

    /// <summary>
    /// Marks a task as completed.
    /// </summary>
    [Description("Mark a task as completed. This allows the campaign to progress to the next task.")]
    public async Task<string> CompleteTask(
        [Description("Task identifier")] Guid taskId)
    {
        this._logger.LogInformation("Agent completing task: {TaskId}", taskId);

        Result<CampaignTask> result = await this._taskService.CompleteTask(taskId);

        if (!result.IsSuccess)
        {
            this._logger.LogError("Failed to complete task: {Error}", result.Error);
            return $"Error: {result.Error}";
        }

        return $"Task completed successfully: {result.Value.Title}";
    }

    /// <summary>
    /// Gets the current campaign status.
    /// </summary>
    [Description("Get the current status of the campaign including phase, tasks, and progress.")]
    public async Task<string> GetCampaignStatus(
        [Description("Campaign identifier")] Guid campaignId)
    {
        this._logger.LogInformation("Agent requesting campaign status: {CampaignId}", campaignId);

        Campaign? campaign = await this._campaignRepository.LoadComplete(campaignId);

        if (campaign == null)
        {
            return $"Error: Campaign {campaignId} not found";
        }

        var status = new
        {
            campaign.Id,
            campaign.Name,
            Phase = campaign.Phase.ToString(),
            TotalTasks = campaign.Tasks.Count,
            CompletedTasks = campaign.Tasks.Count(t => t.Status == Domain.Entities.TaskStatus.Completed),
            PendingTasks = campaign.Tasks.Count(t => t.Status == Domain.Entities.TaskStatus.Pending),
            TotalLeads = campaign.Leads.Count,
            ScoredLeads = campaign.Leads.Count(l => l.Score.HasValue),
        };

        return JsonSerializer.Serialize(status, CampaignAgentTools.JsonOptions);
    }

    /// <summary>
    /// Discovers leads for the campaign.
    /// </summary>
    [Description("Discover and add leads to the campaign. This tool simulates lead discovery.")]
    public async Task<string> DiscoverLeads(
        [Description("Campaign identifier")] Guid campaignId,
        [Description("Search criteria or description of target leads")] string criteria,
        [Description("Number of leads to discover")] int count = 10)
    {
        this._logger.LogInformation(
            "Agent discovering {Count} leads for campaign {CampaignId} with criteria: {Criteria}",
            count,
            campaignId,
            criteria);

        Campaign? campaign = await this._campaignRepository.FindById(campaignId);

        if (campaign == null)
        {
            return $"Error: Campaign {campaignId} not found";
        }

        // Simulate lead discovery (in production, this would call external APIs)
        List<Lead> leads = [];
        for (int i = 0; i < count; i++)
        {
            string leadData = JsonSerializer.Serialize(new
            {
                Name = $"Lead {i + 1}",
                Company = $"Company {i + 1}",
                Criteria = criteria,
                GeneratedAt = DateTime.UtcNow
            });

            var lead = new Lead(
                Guid.NewGuid(),
                campaignId,
                "Simulated Discovery",
                leadData,
                DateTime.UtcNow);

            leads.Add(lead);
            campaign.Leads.Add(lead);
        }

        await this._campaignRepository.SaveChanges();

        // Log event
        var leadsEvent = new LeadsDiscoveredEvent(campaignId, count, "Simulated Discovery");
        await this._eventLog.Append(leadsEvent);

        return $"Successfully discovered {count} leads for campaign {campaign.Name}";
    }

    /// <summary>
    /// Scores a lead.
    /// </summary>
    [Description("Score a lead based on fit and priority. Score should be between 0 and 100.")]
    public async Task<string> ScoreLead(
        [Description("Lead identifier")] Guid leadId,
        [Description("Score value (0-100)")] decimal score,
        [Description("Rationale for the score")] string rationale)
    {
        this._logger.LogInformation(
            "Agent scoring lead {LeadId} with score {Score}",
            leadId,
            score);

        if (score < 0 || score > 100)
        {
            return "Error: Score must be between 0 and 100";
        }

        // In production, this would use a lead repository
        // For now, this demonstrates the pattern
        return $"Lead {leadId} scored successfully with {score}/100";
    }
}
