// -----------------------------------------------------------------------
// <copyright file="CampaignAgentTools.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated public classes", Justification = "Instantiated via dependency injection")]
public sealed class CampaignAgentTools
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly ICampaignRepository campaignRepository;
    private readonly ITaskService taskService;
    private readonly IEventLog eventLog;
    private readonly ILogger<CampaignAgentTools> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CampaignAgentTools"/> class.
    /// </summary>
    public CampaignAgentTools(
        ICampaignRepository campaignRepository,
        ITaskService taskService,
        IEventLog eventLog,
        ILogger<CampaignAgentTools> logger)
    {
        this.campaignRepository = campaignRepository;
        this.taskService = taskService;
        this.eventLog = eventLog;
        this.logger = logger;
    }

    /// <summary>
    /// Creates a new campaign.
    /// </summary>
    [Description("Create a new marketing or sales campaign. This is the first step to start working on a campaign. Returns the campaign ID that you must use in subsequent tool calls.")]
    public async Task<string> CreateCampaign(
        [Description("Campaign name")] string name,
        [Description("Campaign description or goals")] string description)
    {
        this.logger.LogInformation("Agent creating campaign: {Name}", name);

        DateTime now = DateTime.UtcNow;
        Campaign campaign = new(
            Guid.NewGuid(),
            name,
            CampaignPhase.Planning,
            now,
            description);

        await this.campaignRepository.Add(campaign);

        // Log event
        var campaignEvent = new CampaignCreatedEvent(campaign.Id, name);
        await this.eventLog.Append(campaignEvent);

        // Return the GUID directly for easier parsing by LLM
        return $"Campaign created successfully with ID: {campaign.Id}. Use this exact ID in subsequent tool calls (CreateTask, DiscoverLeads, etc.)";
    }

    /// <summary>
    /// Creates a new task for the campaign.
    /// </summary>
    [Description("Create a new task in the campaign. Tasks are executed in order and cannot be skipped.")]
    public async Task<string> CreateTask(
        [Description("Campaign identifier (GUID from CreateCampaign)")] string campaignId,
        [Description("Task title")] string title,
        [Description("Detailed task description")] string description,
        [Description("Whether this task requires user approval")] bool requiresApproval = false)
    {
        if (!Guid.TryParse(campaignId, out Guid campaignGuid))
        {
            return $"Error: Invalid campaign ID '{campaignId}'. Must be a valid GUID.";
        }

        this.logger.LogInformation(
            "Agent creating task: {Title} for campaign {CampaignId}",
            title,
            campaignGuid);

        Result<CampaignTask> result = await this.taskService.CreateTask(
            campaignGuid,
            title,
            description,
            requiresApproval);

        if (!result.IsSuccess)
        {
            this.logger.LogError("Failed to create task: {Error}", result.ErrorMessage);
            return $"Error: {result.ErrorMessage}";
        }

        return $"Task created successfully: {title} (ID: {result.Value.Id})";
    }

    /// <summary>
    /// Marks a task as completed.
    /// </summary>
    [Description("Mark a task as completed. This allows the campaign to progress to the next task.")]
    public async Task<string> CompleteTask(
        [Description("Task identifier (GUID from CreateTask)")] string taskId)
    {
        if (!Guid.TryParse(taskId, out Guid taskGuid))
        {
            return $"Error: Invalid task ID '{taskId}'. Must be a valid GUID.";
        }

        this.logger.LogInformation("Agent completing task: {TaskId}", taskGuid);

        Result<CampaignTask> result = await this.taskService.CompleteTask(taskGuid);

        if (!result.IsSuccess)
        {
            this.logger.LogError("Failed to complete task: {Error}", result.ErrorMessage);
            return $"Error: {result.ErrorMessage}";
        }

        return $"Task completed successfully: {result.Value.Title}";
    }

    /// <summary>
    /// Gets the current campaign status.
    /// </summary>
    [Description("Get the current status of the campaign including phase, tasks, and progress.")]
    public async Task<string> GetCampaignStatus(
        [Description("Campaign identifier (GUID from CreateCampaign)")] string campaignId)
    {
        if (!Guid.TryParse(campaignId, out Guid campaignGuid))
        {
            return $"Error: Invalid campaign ID '{campaignId}'. Must be a valid GUID.";
        }

        this.logger.LogInformation("Agent requesting campaign status: {CampaignId}", campaignGuid);

        Campaign? campaign = await this.campaignRepository.LoadComplete(campaignGuid);

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
        [Description("Campaign identifier (GUID from CreateCampaign)")] string campaignId,
        [Description("Search criteria or description of target leads")] string criteria,
        [Description("Number of leads to discover")] int count = 10)
    {
        if (!Guid.TryParse(campaignId, out Guid campaignGuid))
        {
            return $"Error: Invalid campaign ID '{campaignId}'. Must be a valid GUID.";
        }

        this.logger.LogInformation(
            "Agent discovering {Count} leads for campaign {CampaignId} with criteria: {Criteria}",
            count,
            campaignGuid,
            criteria);

        Campaign? campaign = await this.campaignRepository.FindById(campaignGuid);

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
                GeneratedAt = DateTime.UtcNow,
            });

            var lead = new Lead(
                Guid.NewGuid(),
                campaignGuid,
                "Simulated Discovery",
                leadData,
                DateTime.UtcNow);

            leads.Add(lead);
            campaign.Leads.Add(lead);
        }

        await this.campaignRepository.Update(campaign);

        // Log event
        var leadsEvent = new LeadsDiscoveredEvent(campaignGuid, count, "Simulated Discovery");
        await this.eventLog.Append(leadsEvent);

        return $"Successfully discovered {count} leads for campaign {campaign.Name}";
    }

    /// <summary>
    /// Scores a lead.
    /// </summary>
    [Description("Score a lead based on fit and priority. Score should be between 0 and 100.")]
    public async Task<string> ScoreLead(
        [Description("Lead identifier (GUID)")] string leadId,
        [Description("Score value (0-100)")] decimal score,
        [Description("Rationale for the score")] string rationale)
    {
        if (!Guid.TryParse(leadId, out Guid leadGuid))
        {
            return $"Error: Invalid lead ID '{leadId}'. Must be a valid GUID.";
        }

        this.logger.LogInformation(
            "Agent scoring lead {LeadId} with score {Score}",
            leadGuid,
            score);

        if (score < 0 || score > 100)
        {
            return "Error: Score must be between 0 and 100";
        }

        // Find the campaign that contains this lead
        IEnumerable<Campaign> campaigns = await this.campaignRepository.GetAll();
        Campaign? campaign = null;
        Lead? lead = null;

        foreach (Campaign c in campaigns)
        {
            Campaign? fullCampaign = await this.campaignRepository.LoadComplete(c.Id);
            if (fullCampaign != null)
            {
                lead = fullCampaign.Leads.FirstOrDefault(l => l.Id == leadGuid);
                if (lead != null)
                {
                    campaign = fullCampaign;
                    break;
                }
            }
        }

        if (lead == null || campaign == null)
        {
            return $"Error: Lead {leadGuid} not found";
        }

        lead.Score = score;
        lead.ScoringRationale = rationale;
        lead.ScoredAt = DateTime.UtcNow;

        await this.campaignRepository.Update(campaign);

        // Log event
        var scoreEvent = new LeadScoredEvent(leadGuid, campaign.Id, score, rationale);
        await this.eventLog.Append(scoreEvent);

        return $"Lead {leadGuid} scored successfully with {score}/100. Rationale: {rationale}";
    }
}

