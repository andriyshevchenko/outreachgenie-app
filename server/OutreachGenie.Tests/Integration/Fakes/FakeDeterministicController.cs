// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using OutreachGenie.Application.Services;

namespace OutreachGenie.Tests.Integration.Fakes;

/// <summary>
/// Fake DeterministicController for testing.
/// Tracks ExecuteTaskWithLlmAsync calls without real execution.
/// </summary>
internal sealed class FakeDeterministicController
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
}
