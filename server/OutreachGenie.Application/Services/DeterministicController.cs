using OutreachGenie.Application.Interfaces;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;
using TaskStatus = OutreachGenie.Domain.Enums.TaskStatus;

namespace OutreachGenie.Application.Services;

/// <summary>
/// Deterministic controller enforcing "Agent proposes, Controller decides" architecture.
/// Reloads state at start of every cycle, validates proposals, executes actions, persists audit logs.
/// </summary>
public sealed class DeterministicController
{
    private readonly ICampaignRepository campaigns;
    private readonly ITaskRepository tasks;
    private readonly IArtifactRepository artifacts;
    private readonly ILeadRepository leads;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicController"/> class.
    /// </summary>
    /// <param name="campaigns">Campaign repository.</param>
    /// <param name="tasks">Task repository.</param>
    /// <param name="artifacts">Artifact repository.</param>
    /// <param name="leads">Lead repository.</param>
    public DeterministicController(
        ICampaignRepository campaigns,
        ITaskRepository tasks,
        IArtifactRepository artifacts,
        ILeadRepository leads)
    {
        this.campaigns = campaigns;
        this.tasks = tasks;
        this.artifacts = artifacts;
        this.leads = leads;
    }

    /// <summary>
    /// Selects next eligible task for execution based on campaign state.
    /// </summary>
    /// <param name="state">Current campaign state.</param>
    /// <returns>Next task to execute, or null if none eligible.</returns>
    public static CampaignTask? SelectNextTask(CampaignState state)
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
    public static ValidationResult ValidateProposal(ActionProposal proposal, CampaignState state)
    {
        if (proposal.TaskId.HasValue)
        {
            var task = state.Tasks.FirstOrDefault(t => t.Id == proposal.TaskId.Value);
            if (task == null)
            {
                return ValidationResult.Failure($"Task {proposal.TaskId} not found in campaign {state.Campaign.Id}");
            }

            if (task.Status != TaskStatus.Pending && task.Status != TaskStatus.Retrying)
            {
                return ValidationResult.Failure($"Task {task.Id} has status {task.Status}, cannot execute");
            }
        }

        if (state.Campaign.Status != CampaignStatus.Active)
        {
            return ValidationResult.Failure($"Campaign {state.Campaign.Id} is not active, current status: {state.Campaign.Status}");
        }

        return ValidationResult.Success();
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
}
