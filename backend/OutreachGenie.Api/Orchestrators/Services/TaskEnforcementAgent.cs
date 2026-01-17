// -----------------------------------------------------------------------
// <copyright file="TaskEnforcementAgent.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OutreachGenie.Api.Domain.Services;

namespace OutreachGenie.Api.Orchestrators.Services;

/// <summary>
/// Middleware that enforces task completion before allowing progression.
/// This is the critical component that solves Section 9 Issue #1 (LLM skipping steps).
/// </summary>
public sealed class TaskEnforcementAgent : DelegatingAIAgent
{
    private readonly ITaskService taskService;
    private readonly ILogger<TaskEnforcementAgent> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskEnforcementAgent"/> class.
    /// </summary>
    public TaskEnforcementAgent(
        AIAgent innerAgent,
        ITaskService taskService,
        ILogger<TaskEnforcementAgent> logger)
        : base(innerAgent)
    {
        this.taskService = taskService;
        this.logger = logger;
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<AgentRunResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Extract campaign ID from options or thread state
        Guid? campaignId = ExtractCampaignId();

        if (campaignId.HasValue)
        {
            // CRITICAL ENFORCEMENT: Check for next required task
            Domain.Entities.CampaignTask? nextTask = await this.taskService.NextRequiredTask(campaignId.Value, cancellationToken);

            if (nextTask != null)
            {
                this.logger.LogInformation(
                    "Task enforcement: Injecting pending task {TaskId} into system message",
                    nextTask.Id);

                // Inject task context into system message
                string taskContext = $"""
                    
                    IMPORTANT - NEXT REQUIRED TASK:
                    There is a pending task that must be completed before proceeding:
                    
                    Task: {nextTask.Title}
                    Description: {nextTask.Description ?? "No description"}
                    Status: {nextTask.Status}
                    
                    You MUST focus on completing this task first. Do not skip ahead to other tasks.
                    Use the appropriate tools to complete this task, then mark it as complete.
                    """;

                // Prepend system message with task context
                List<ChatMessage> messagesList = [.. messages];
                ChatMessage? systemMessage = messagesList.FirstOrDefault(m => m.Role == ChatRole.System);

                if (systemMessage != null)
                {
                    // Create new message with combined content since ChatMessage.Text is read-only
                    messagesList.Remove(systemMessage);
                    messagesList.Insert(0, new ChatMessage(ChatRole.System, systemMessage.Text + taskContext));
                }
                else
                {
                    messagesList.Insert(0, new ChatMessage(ChatRole.System, taskContext));
                }

                messages = messagesList;
            }
        }

        // Call inner agent and log all interactions
        await foreach (AgentRunResponseUpdate update in base.RunCoreStreamingAsync(messages, thread, options, cancellationToken))
        {
            // Future: Add tool call logging when API supports it
            // Currently AgentRunResponseUpdate doesn't expose ToolCallUpdates in the current SDK version

            yield return update;
        }
    }

    /// <summary>
    /// Extracts campaign ID from run options or thread state.
    /// </summary>
    private static Guid? ExtractCampaignId()
    {
        // Campaign ID extraction not yet implemented
        // Task enforcement will be disabled until context passing is added
        return null;
    }
}

