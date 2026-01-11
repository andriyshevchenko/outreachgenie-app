// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json;

namespace OutreachGenie.Application.Services.Mcp;

/// <summary>
/// Represents a communication transport for MCP server interactions.
/// Handles the underlying protocol for sending requests and receiving responses.
/// Implementations include stdio (standard input/output) and HTTP transports.
/// </summary>
/// <example>
/// var transport = new StdioMcpTransport("npx", "-y @modelcontextprotocol/server-playwright");
/// await transport.ConnectAsync();
/// var response = await transport.SendRequestAsync(request);
/// await transport.DisconnectAsync();
/// </example>
public interface IMcpTransport
{
    /// <summary>
    /// Gets a value indicating whether the transport is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Establishes the transport connection.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the connection operation.</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the transport connection.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the disconnection operation.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a JSON-RPC request and waits for the response.
    /// </summary>
    /// <param name="request">The JSON-RPC request to send.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The JSON-RPC response.</returns>
    Task<JsonDocument> SendAsync(
        JsonDocument request,
        CancellationToken cancellationToken = default);
}
