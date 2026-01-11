// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json;
using OutreachGenie.Application.Services.Mcp;

namespace OutreachGenie.Tests.Integration.Fakes;

/// <summary>
/// Fake MCP tool registry for testing.
/// Returns empty tool lists for predictable testing.
/// </summary>
internal sealed class FakeMcpToolRegistry : IMcpToolRegistry
{
    /// <summary>
    /// Registers an MCP server (no-op for testing).
    /// </summary>
    /// <param name="server">The MCP server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    public Task RegisterAsync(IMcpServer server, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Unregisters an MCP server (no-op for testing).
    /// </summary>
    /// <param name="id">The server identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    public Task UnregisterAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Discovers tools asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty list of MCP tools.</returns>
    public Task<IReadOnlyList<McpTool>> DiscoverToolsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<McpTool>>(new List<McpTool>());
    }

    /// <summary>
    /// Finds a tool by name.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Null (no tools available).</returns>
    public Task<McpTool?> FindToolAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<McpTool?>(null);
    }

    /// <summary>
    /// Validates tool parameters (always returns true for testing).
    /// </summary>
    /// <param name="tool">The tool to validate.</param>
    /// <param name="parameters">The parameters to validate.</param>
    /// <returns>Always true.</returns>
    public bool Validate(McpTool tool, JsonDocument parameters)
    {
        return true;
    }

    /// <summary>
    /// Gets all MCP servers.
    /// </summary>
    /// <returns>Empty list of MCP servers.</returns>
    public IReadOnlyList<IMcpServer> All()
    {
        return new List<IMcpServer>();
    }
}
