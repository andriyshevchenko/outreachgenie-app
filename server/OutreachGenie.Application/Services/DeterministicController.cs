using System.Text.Json;
using OutreachGenie.Application.Interfaces;
using OutreachGenie.Application.Services.Llm;
using OutreachGenie.Application.Services.Mcp;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;
using TaskStatus = OutreachGenie.Domain.Enums.TaskStatus;

namespace OutreachGenie.Application.Services;

/// <summary>
/// Deterministic controller enforcing "Agent proposes, Controller decides" architecture.
/// Reloads state at start of every cycle, validates proposals, executes actions, persists audit logs.
/// LLM receives campaign state and available MCP tools, proposes tool calls dynamically.
/// </summary>
public sealed class DeterministicController
{
    private readonly ICampaignRepository campaigns;
    private readonly ITaskRepository tasks;
    private readonly IArtifactRepository artifacts;
    private readonly ILeadRepository leads;
    private readonly ILlmProvider llm;
    private readonly IMcpToolRegistry registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicController"/> class.
    /// </summary>
    /// <param name="campaigns">Campaign repository.</param>
    /// <param name="tasks">Task repository.</param>
    /// <param name="artifacts">Artifact repository.</param>
    /// <param name="leads">Lead repository.</param>
    /// <param name="llm">LLM provider for generating action proposals.</param>
    /// <param name="registry">MCP tool registry for discovering and executing tools.</param>
    public DeterministicController(
        ICampaignRepository campaigns,
        ITaskRepository tasks,
        IArtifactRepository artifacts,
        ILeadRepository leads,
        ILlmProvider llm,
        IMcpToolRegistry registry)
    {
        this.campaigns = campaigns;
        this.tasks = tasks;
        this.artifacts = artifacts;
        this.leads = leads;
        this.llm = llm;
        this.registry = registry;
    }

    /// <summary>
    /// Selects next eligible task for execution based on campaign state.
    /// </summary>
    /// <param name="state">Current campaign state.</param>
    /// <returns>Next task to execute, or null if none eligible.</returns>
#pragma warning disable S2325 // Make method static - violates Elegant Objects principle [C13-C]
    public CampaignTask? SelectNextTask(CampaignState state)
#pragma warning restore S2325
    {
        if (state.Campaign.Status != CampaignStatus.Active)
        {
            return null;
        }

        var pendingTasks = state.Tasks
            .Where(t => t.Status == TaskStatus.Pending)
            .OrderBy(t => t.CreatedAt)
            .ToList();

        return pendingTasks.FirstOrDefault();
    }

    /// <summary>
    /// Validates LLM proposal against current state and business rules.
    /// </summary>
    /// <param name="proposal">Proposed action from LLM.</param>
    /// <param name="state">Current campaign state.</param>
    /// <returns>Validation result.</returns>
#pragma warning disable S2325 // Make method static - violates Elegant Objects principle [C13-C]
    public ValidationResult ValidateProposal(ActionProposal proposal, CampaignState state)
#pragma warning restore S2325
    {
        if (proposal.TaskId.HasValue)
        {
            var task = state.Tasks.FirstOrDefault(t => t.Id == proposal.TaskId.Value);
            if (task == null)
            {
                return new ValidationResult(false, $"Task {proposal.TaskId} not found in campaign {state.Campaign.Id}");
            }

            if (task.Status != TaskStatus.Pending && task.Status != TaskStatus.Retrying)
            {
                return new ValidationResult(false, $"Task {task.Id} has status {task.Status}, cannot execute");
            }
        }

        if (state.Campaign.Status != CampaignStatus.Active)
        {
            return new ValidationResult(false, $"Campaign {state.Campaign.Id} is not active, current status: {state.Campaign.Status}");
        }

        return new ValidationResult(true);
    }

    /// <summary>
    /// Reloads complete campaign state from persistent storage.
    /// </summary>
    /// <param name="campaignId">Campaign identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Campaign state with all related entities.</returns>
    public async Task<CampaignState> ReloadStateAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await this.campaigns.GetWithAllRelatedAsync(campaignId, cancellationToken);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        var campaignTasks = await this.tasks.GetByCampaignIdAsync(campaignId, cancellationToken);
        var campaignArtifacts = await this.artifacts.GetByCampaignIdAsync(campaignId, cancellationToken);
        var campaignLeads = await this.leads.GetByCampaignIdAsync(campaignId, cancellationToken);

        return new CampaignState(campaign, campaignTasks, campaignArtifacts, campaignLeads);
    }

    /// <summary>
    /// Updates task status and metadata after execution.
    /// </summary>
    /// <param name="task">Task to update.</param>
    /// <param name="newStatus">New status.</param>
    /// <param name="output">Execution output.</param>
    /// <param name="error">Error message if failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    public async Task UpdateTaskAfterExecutionAsync(
        CampaignTask task,
        TaskStatus newStatus,
        string? output = null,
        string? error = null,
        CancellationToken cancellationToken = default)
    {
        task.Status = newStatus;

        if (output != null)
        {
            task.OutputJson = output;
        }

        if (error != null)
        {
            task.ErrorMessage = error;
        }

        if (newStatus == TaskStatus.Done)
        {
            task.CompletedAt = DateTime.UtcNow;
        }
        else if (newStatus == TaskStatus.Failed && task.RetryCount < task.MaxRetries)
        {
            task.Status = TaskStatus.Retrying;
            task.RetryCount++;
        }

        await this.tasks.UpdateAsync(task, cancellationToken);
    }

    /// <summary>
    /// Creates audit log entry for executed action.
    /// </summary>
    /// <param name="campaignId">Campaign identifier.</param>
    /// <param name="action">Action type.</param>
    /// <param name="details">Action details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audit log artifact.</returns>
    public async Task<Artifact> CreateAuditLogAsync(
        Guid campaignId,
        string action,
        string details,
        CancellationToken cancellationToken = default)
    {
        var log = new Artifact
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Type = ArtifactType.Arbitrary,
            Key = $"audit_log_{DateTime.UtcNow:yyyyMMddHHmmss}",
            ContentJson = $"{{\"action\":\"{action}\",\"timestamp\":\"{DateTime.UtcNow:O}\",\"details\":{details}}}",
            Source = ArtifactSource.Agent,
            CreatedAt = DateTime.UtcNow,
        };

        return await this.artifacts.CreateAsync(log, cancellationToken);
    }

    /// <summary>
    /// Transitions campaign to new status with validation.
    /// </summary>
    /// <param name="campaignId">Campaign identifier.</param>
    /// <param name="newStatus">Target status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    public async Task TransitionCampaignStatusAsync(
        Guid campaignId,
        CampaignStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        var campaign = await this.campaigns.GetByIdAsync(campaignId, cancellationToken);
        if (campaign == null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found");
        }

        if (!IsValidTransition(campaign.Status, newStatus))
        {
            throw new InvalidOperationException($"Invalid status transition: {campaign.Status} -> {newStatus}");
        }

        campaign.Status = newStatus;
        campaign.UpdatedAt = DateTime.UtcNow;

        if (newStatus == CampaignStatus.Completed || newStatus == CampaignStatus.Error)
        {
            campaign.CompletedAt = DateTime.UtcNow;
        }

        await this.campaigns.UpdateAsync(campaign, cancellationToken);
    }

    /// <summary>
    /// Executes task using LLM-driven orchestration with MCP tools.
    /// Loads state, gets available tools, asks LLM for proposals, validates, executes until task complete.
    /// </summary>
    /// <param name="taskId">Task identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    public async Task ExecuteTaskWithLlmAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await this.tasks.GetByIdAsync(taskId, cancellationToken);
        if (task == null)
        {
            throw new InvalidOperationException($"Task {taskId} not found");
        }

        var state = await this.ReloadStateAsync(task.CampaignId, cancellationToken);
        var validation = this.ValidateProposal(new ActionProposal { TaskId = taskId }, state);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Task {taskId} is not eligible for execution");
        }

        task.Status = TaskStatus.InProgress;
        task.StartedAt = DateTime.UtcNow;
        await this.tasks.UpdateAsync(task, cancellationToken);

        var tools = await this.registry.DiscoverToolsAsync(cancellationToken);
        var prompt = $"Execute task: {task.Description}. Task type: {task.Type}. Use available MCP tools to complete this task.";
        var maxIterations = 20;
        var iteration = 0;
        var consecutiveErrors = 0;
        var maxConsecutiveErrors = 3;
        while (iteration < maxIterations)
        {
            iteration++;
            try
            {
                state = await this.ReloadStateAsync(task.CampaignId, cancellationToken);
                ActionProposal? proposal = null;
                var retryCount = 0;
                var maxRetries = 3;
                while (retryCount < maxRetries)
                {
                    try
                    {
                        proposal = await this.llm.GenerateProposalAsync(state, tools, prompt, cancellationToken);
                        break;
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        if (retryCount >= maxRetries)
                        {
                            throw new InvalidOperationException($"LLM failed after {maxRetries} retries: {ex.Message}", ex);
                        }

                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
                    }
                }

                if (proposal == null || string.IsNullOrEmpty(proposal.ActionType))
                {
                    consecutiveErrors++;
                    if (consecutiveErrors >= maxConsecutiveErrors)
                    {
                        throw new InvalidOperationException($"LLM returned invalid proposals {consecutiveErrors} times consecutively");
                    }

                    await this.CreateAuditLogAsync(
                        task.CampaignId,
                        "invalid_proposal",
                        $"{{\"iteration\":{iteration},\"error\":\"LLM returned null or empty ActionType\"}}",
                        cancellationToken);
                    continue;
                }

                consecutiveErrors = 0;
                if (proposal.ActionType.Equals("task_complete", StringComparison.OrdinalIgnoreCase))
                {
                    await this.UpdateTaskAfterExecutionAsync(
                        task,
                        TaskStatus.Done,
                        proposal.Parameters,
                        null,
                        cancellationToken);
                    await this.CreateAuditLogAsync(
                        task.CampaignId,
                        "task_complete",
                        $"{{\"task_id\":\"{taskId}\",\"iterations\":{iteration}}}",
                        cancellationToken);
                    return;
                }

                var tool = await this.registry.FindToolAsync(proposal.ActionType, cancellationToken);
                if (tool == null)
                {
                    await this.CreateAuditLogAsync(
                        task.CampaignId,
                        "invalid_tool",
                        $"{{\"tool_name\":\"{proposal.ActionType}\",\"task_id\":\"{taskId}\",\"iteration\":{iteration}}}",
                        cancellationToken);
                    prompt = $"Error: Tool '{proposal.ActionType}' not found. Available tools: {string.Join(", ", tools.Select(t => t.Name))}. Continue executing task: {task.Description}.";
                    continue;
                }

                if (!string.IsNullOrEmpty(proposal.Parameters))
                {
                    try
                    {
                        JsonDocument.Parse(proposal.Parameters);
                    }
                    catch (JsonException ex)
                    {
                        await this.CreateAuditLogAsync(
                            task.CampaignId,
                            "invalid_parameters",
                            $"{{\"tool_name\":\"{proposal.ActionType}\",\"error\":\"{ex.Message}\",\"iteration\":{iteration}}}",
                            cancellationToken);
                        prompt = $"Error: Invalid JSON parameters for tool '{proposal.ActionType}'. Continue executing task: {task.Description}.";
                        continue;
                    }
                }

                var result = await this.ExecuteToolAsync(proposal, cancellationToken);
                await this.CreateAuditLogAsync(
                    task.CampaignId,
                    proposal.ActionType,
                    $"{{\"tool_name\":\"{proposal.ActionType}\",\"parameters\":{proposal.Parameters ?? "null"},\"result\":{result},\"iteration\":{iteration}}}",
                    cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                prompt = $"Previous action: {proposal.ActionType}. Result: {result}. Continue executing task: {task.Description}.";
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                consecutiveErrors++;
                if (consecutiveErrors >= maxConsecutiveErrors)
                {
                    await this.UpdateTaskAfterExecutionAsync(
                        task,
                        TaskStatus.Failed,
                        null,
                        $"Failed after {consecutiveErrors} consecutive errors: {ex.Message}",
                        cancellationToken);
                    throw;
                }

                await this.CreateAuditLogAsync(
                    task.CampaignId,
                    "execution_error",
                    $"{{\"iteration\":{iteration},\"error\":\"{ex.Message}\",\"type\":\"{ex.GetType().Name}\"}}",
                    cancellationToken);
                prompt = $"Error occurred: {ex.Message}. Try alternative approach to execute task: {task.Description}.";
            }
        }

        await this.UpdateTaskAfterExecutionAsync(
            task,
            TaskStatus.Failed,
            null,
            $"Exceeded maximum iterations ({maxIterations})",
            cancellationToken);
    }

    /// <summary>
    /// Validates campaign status transition.
    /// </summary>
    /// <param name="current">Current status.</param>
    /// <param name="target">Target status.</param>
    /// <returns>True if transition is valid.</returns>
    private static bool IsValidTransition(CampaignStatus current, CampaignStatus target)
    {
        return (current, target) switch
        {
            (CampaignStatus.Initializing, CampaignStatus.Active) => true,
            (CampaignStatus.Initializing, CampaignStatus.Paused) => true,
            (CampaignStatus.Active, CampaignStatus.Paused) => true,
            (CampaignStatus.Active, CampaignStatus.Completed) => true,
            (CampaignStatus.Active, CampaignStatus.Error) => true,
            (CampaignStatus.Paused, CampaignStatus.Active) => true,
            (CampaignStatus.Paused, CampaignStatus.Error) => true,
            _ => false,
        };
    }

    /// <summary>
    /// Executes MCP tool based on LLM proposal.
    /// Finds tool in registry, validates parameters, calls tool, returns result.
    /// </summary>
    /// <param name="proposal">Action proposal from LLM.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tool execution result as JSON.</returns>
    private async Task<string> ExecuteToolAsync(ActionProposal proposal, CancellationToken cancellationToken)
    {
        var tool = await this.registry.FindToolAsync(proposal.ActionType, cancellationToken);
        if (tool == null)
        {
            return $"{{\"error\":\"Tool {proposal.ActionType} not found\"}}";
        }

        var parameters = JsonDocument.Parse(proposal.Parameters ?? "{}");
        var servers = this.registry.All();
        foreach (var server in servers)
        {
            var serverTools = await server.ListToolsAsync(cancellationToken);
            if (serverTools.Any(t => t.Name == proposal.ActionType))
            {
                var result = await server.CallToolAsync(proposal.ActionType, parameters, cancellationToken);
                return JsonSerializer.Serialize(result);
            }
        }

        return $"{{\"error\":\"No server provides tool {proposal.ActionType}\"}}";
    }
}
