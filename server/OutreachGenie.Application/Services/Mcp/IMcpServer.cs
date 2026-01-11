// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json;

namespace OutreachGenie.Application.Services.Mcp;

/// <summary>
/// Represents an MCP (Model Context Protocol) server connection.
/// Provides methods to connect, disconnect, list available tools, and execute tool calls.
/// MCP servers are external processes that expose tools via stdio or HTTP transport.
/// </summary>
/// <example>
/// var server = new DesktopCommanderMcpServer();
/// await server.ConnectAsync();
/// var tools = await server.ListToolsAsync();
/// var result = await server.CallToolAsync("read_file", parameters);
/// await server.DisconnectAsync();
/// </example>
public interface IMcpServer
{
    /// <summary>
    /// Gets the unique identifier of this MCP server.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of this MCP server.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a value indicating whether the server is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Establishes connection to the MCP server.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the connection operation.</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the connection to the MCP server.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the disconnection operation.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the list of tools exposed by this MCP server.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of tool definitions.</returns>
    Task<IReadOnlyList<McpTool>> ListToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a tool with the specified parameters.
    /// </summary>
    /// <param name="name">The name of the tool to invoke.</param>
    /// <param name="parameters">JSON document containing tool parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>JSON document containing the tool execution result.</returns>
    Task<JsonDocument> CallToolAsync(
        string name,
        JsonDocument parameters,
        CancellationToken cancellationToken = default);
}
