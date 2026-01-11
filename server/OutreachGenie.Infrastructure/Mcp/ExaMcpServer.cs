// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json;
using Microsoft.Extensions.Logging;
using OutreachGenie.Application.Interfaces.Mcp;
using OutreachGenie.Application.Services.Mcp;

namespace OutreachGenie.Infrastructure.Mcp;

/// <summary>
/// MCP server implementation for Exa web search.
/// Provides semantic search and content discovery capabilities.
/// </summary>
public sealed class ExaMcpServer : IMcpServer
{
    private readonly IMcpTransport transport;
    private readonly ILogger<ExaMcpServer> logger;
    private int requestId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExaMcpServer"/> class.
    /// </summary>
    /// <param name="transport">The transport for communication.</param>
    /// <param name="logger">Logger for server operations.</param>
    public ExaMcpServer(IMcpTransport transport, ILogger<ExaMcpServer> logger)
    {
        this.transport = transport;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public string Id => "exa";

    /// <inheritdoc/>
    public string Name => "Exa Web Search";

    /// <inheritdoc/>
    public bool IsConnected => this.transport.IsConnected;

    /// <inheritdoc/>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Connecting to Exa MCP server");
        await this.transport.ConnectAsync(cancellationToken);

        var initRequest = CreateRequest("initialize", new
        {
            protocolVersion = "2024-11-05",
            capabilities = new { },
            clientInfo = new
            {
                name = "OutreachGenie",
                version = "1.0.0",
            },
        });

        await this.transport.SendAsync(initRequest, cancellationToken);
        this.logger.LogInformation("Exa MCP server initialized");
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Disconnecting from Exa MCP server");
        await this.transport.DisconnectAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<McpTool>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        var request = CreateRequest("tools/list", new { });
        var response = await this.transport.SendAsync(request, cancellationToken);

        var tools = new List<McpTool>();
        if (response.RootElement.TryGetProperty("result", out var result) &&
            result.TryGetProperty("tools", out var toolsArray))
        {
            foreach (var tool in toolsArray.EnumerateArray())
            {
                var name = tool.GetProperty("name").GetString() ?? string.Empty;
                var description = tool.TryGetProperty("description", out var desc)
                    ? desc.GetString() ?? string.Empty
                    : string.Empty;

                var schema = tool.TryGetProperty("inputSchema", out var schemaElement)
                    ? JsonDocument.Parse(schemaElement.GetRawText())
                    : JsonDocument.Parse("{}");

                tools.Add(new McpTool(name, description, schema));
            }
        }

        this.logger.LogInformation("Retrieved {ToolCount} tools from Exa", tools.Count);
        return tools;
    }

    /// <inheritdoc/>
    public async Task<JsonDocument> CallToolAsync(
        string name,
        JsonDocument parameters,
        CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Calling Exa tool: {ToolName}", name);

        var request = CreateRequest("tools/call", new
        {
            name,
            arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(parameters),
        });

        var response = await this.transport.SendAsync(request, cancellationToken);

        if (response.RootElement.TryGetProperty("error", out var error))
        {
            var errorMessage = error.TryGetProperty("message", out var msg)
                ? msg.GetString()
                : "Unknown error";
            throw new InvalidOperationException($"MCP tool call failed: {errorMessage}");
        }

        return response;
    }

    private JsonDocument CreateRequest(string method, object parameters)
    {
        var id = Interlocked.Increment(ref this.requestId);
        var request = new
        {
            jsonrpc = "2.0",
            id,
            method,
            @params = parameters,
        };

        var json = JsonSerializer.Serialize(request);
        return JsonDocument.Parse(json);
    }
}
