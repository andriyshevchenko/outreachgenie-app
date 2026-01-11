// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using OutreachGenie.Application.Services;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;
using TaskStatus = OutreachGenie.Domain.Enums.TaskStatus;

namespace OutreachGenie.Tests.Integration.Fakes;

/// <summary>
/// Fake DeterministicController for testing.
/// Tracks ExecuteTaskWithLlmAsync calls without real execution.
/// </summary>
internal sealed class FakeDeterministicController : IDeterministicController
{
    private readonly List<Guid> executedTasks = new();
    private Exception? exceptionToThrow;

    /// <summary>
    /// Gets list of task IDs that were executed.
    /// </summary>
    public IReadOnlyList<Guid> ExecutedTasks => this.executedTasks;

    /// <summary>
    /// Configures exception to throw on next execution.
    /// </summary>
    /// <param name="exception">Exception to throw.</param>
    public void ThrowOnExecute(Exception exception)
    {
        this.exceptionToThrow = exception;
    }

    /// <summary>
    /// Executes a task with LLM (fake implementation that only tracks calls).
    /// </summary>
    /// <param name="taskId">Task identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    public Task ExecuteTaskWithLlmAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        if (this.exceptionToThrow != null)
        {
            var ex = this.exceptionToThrow;
            this.exceptionToThrow = null;
            throw ex;
        }

        this.executedTasks.Add(taskId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reloads complete campaign state from persistent storage (fake implementation).
    /// </summary>
    /// <param name="campaignId">Campaign identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty campaign state.</returns>
    public Task<CampaignState> ReloadStateAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = new Campaign { Id = campaignId, Status = CampaignStatus.Active };
        return Task.FromResult(new CampaignState(campaign, [], [], []));
    }

    /// <summary>
    /// Updates task status and metadata after execution (fake implementation).
    /// </summary>
    /// <param name="task">Task to update.</param>
    /// <param name="newStatus">New status.</param>
    /// <param name="output">Execution output.</param>
    /// <param name="error">Error message if failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    public Task UpdateTaskAfterExecutionAsync(
        CampaignTask task,
        TaskStatus newStatus,
        string? output = null,
        string? error = null,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates audit log entry for executed action (fake implementation).
    /// </summary>
    /// <param name="campaignId">Campaign identifier.</param>
    /// <param name="action">Action type.</param>
    /// <param name="details">Action details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Fake audit log artifact.</returns>
    public Task<Artifact> CreateAuditLogAsync(
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
            CreatedAt = DateTime.UtcNow,
        };
        return Task.FromResult(log);
    }

    /// <summary>
    /// Transitions campaign to new status with validation (fake implementation).
    /// </summary>
    /// <param name="campaignId">Campaign identifier.</param>
    /// <param name="newStatus">Target status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    public Task TransitionCampaignStatusAsync(
        Guid campaignId,
        CampaignStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
