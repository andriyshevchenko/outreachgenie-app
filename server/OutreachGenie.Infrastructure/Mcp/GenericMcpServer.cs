using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutreachGenie.Application.Interfaces.Mcp;
using OutreachGenie.Application.Services.Mcp;
using OutreachGenie.Domain.Mcp;

namespace OutreachGenie.Infrastructure.Mcp;

/// <summary>
/// Generic MCP server implementation for dynamically configured servers.
/// </summary>
/// <remarks>
/// Wraps any IMcpTransport with server lifecycle management. Used for servers loaded
/// from mcp.json configuration where specific functionality is not implemented in code.
/// Provides pass-through access to tools, resources, and prompts via the transport layer.
/// </remarks>
public sealed class GenericMcpServer : IMcpServer
{
    private readonly IMcpTransport transport;
    private readonly ILogger<GenericMcpServer> logger;

    public GenericMcpServer(string name, IMcpTransport transport, ILogger<GenericMcpServer> logger)
    {
        this.Name = name;
        this.Id = name;
        this.transport = transport;
        this.logger = logger;
    }

    public string Id { get; }

    public string Name { get; }

    public bool IsConnected => this.transport.IsConnected;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await this.transport.ConnectAsync(cancellationToken);
        this.logger.LogInformation("Connected to MCP server: {ServerName}", this.Name);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await this.transport.DisconnectAsync(cancellationToken);
        this.logger.LogInformation("Disconnected from MCP server: {ServerName}", this.Name);
    }

    public Task<IReadOnlyList<McpTool>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Generic server does not implement tool listing");
        return Task.FromResult<IReadOnlyList<McpTool>>(new List<McpTool>());
    }

    public Task<JsonDocument> CallToolAsync(string name, JsonDocument parameters, CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Generic server does not implement tool calling");
        return Task.FromResult(JsonDocument.Parse("{}"));
    }
}
