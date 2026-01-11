// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json;

namespace OutreachGenie.Application.Services.Mcp;

/// <summary>
/// Registry for managing and discovering MCP servers and their tools.
/// Provides centralized access to all registered MCP servers and their exposed tools.
/// Validates tool calls against registered schemas before execution.
/// </summary>
/// <example>
/// var registry = new McpToolRegistry();
/// registry.Register(new DesktopCommanderMcpServer());
/// registry.Register(new PlaywrightMcpServer());
/// var tool = await registry.GetToolAsync("read_file");
/// var isValid = registry.ValidateParameters("read_file", parameters).
/// </example>
public interface IMcpToolRegistry
{
    /// <summary>
    /// Registers an MCP server and its tools with the registry.
    /// </summary>
    /// <param name="server">The MCP server to register.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the registration operation.</returns>
    Task RegisterAsync(IMcpServer server, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an MCP server from the registry.
    /// </summary>
    /// <param name="id">The unique identifier of the server to unregister.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    Task UnregisterAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all registered MCP servers.
    /// </summary>
    /// <returns>Collection of registered MCP servers.</returns>
    IReadOnlyList<IMcpServer> All();

    /// <summary>
    /// Discovers all available tools across all connected MCP servers.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of all available tools from all servers.</returns>
    Task<IReadOnlyList<McpTool>> DiscoverToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a tool by its name across all registered MCP servers.
    /// </summary>
    /// <param name="name">Name of the tool to find.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The tool definition if found.</returns>
    Task<McpTool?> FindToolAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a set of parameters conforms to the tool's schema.
    /// </summary>
    /// <param name="tool">The tool whose schema to validate against.</param>
    /// <param name="parameters">The parameters to validate.</param>
    /// <returns>True if parameters are valid, false otherwise.</returns>
    bool Validate(McpTool tool, JsonDocument parameters);
}
