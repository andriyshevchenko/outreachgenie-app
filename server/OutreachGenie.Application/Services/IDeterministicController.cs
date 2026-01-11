using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;
using TaskStatus = OutreachGenie.Domain.Enums.TaskStatus;

namespace OutreachGenie.Application.Services;

/// <summary>
/// Interface for deterministic controller enforcing "Agent proposes, Controller decides" architecture.
/// </summary>
public interface IDeterministicController
{
    /// <summary>
    /// Reloads complete campaign state from persistent storage.
    /// </summary>
    /// <param name="campaignId">Campaign identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Campaign state with all related entities.</returns>
    Task<CampaignState> ReloadStateAsync(Guid campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates task status and metadata after execution.
    /// </summary>
    /// <param name="task">Task to update.</param>
    /// <param name="newStatus">New status.</param>
    /// <param name="output">Execution output.</param>
    /// <param name="error">Error message if failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    Task UpdateTaskAfterExecutionAsync(
        CampaignTask task,
        TaskStatus newStatus,
        string? output = null,
        string? error = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates audit log entry for executed action.
    /// </summary>
    /// <param name="campaignId">Campaign identifier.</param>
    /// <param name="action">Action type.</param>
    /// <param name="details">Action details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audit log artifact.</returns>
    Task<Artifact> CreateAuditLogAsync(
        Guid campaignId,
        string action,
        string details,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions campaign to new status with validation.
    /// </summary>
    /// <param name="campaignId">Campaign identifier.</param>
    /// <param name="newStatus">Target status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    Task TransitionCampaignStatusAsync(
        Guid campaignId,
        CampaignStatus newStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes task using LLM-driven orchestration with MCP tools.
    /// </summary>
    /// <param name="taskId">Task identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    Task ExecuteTaskWithLlmAsync(Guid taskId, CancellationToken cancellationToken = default);
}
