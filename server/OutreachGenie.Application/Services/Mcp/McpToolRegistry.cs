// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json;

namespace OutreachGenie.Application.Services.Mcp;

/// <summary>
/// Concrete implementation of IMcpToolRegistry for managing MCP servers and their tools.
/// Thread-safe registry with concurrent access support.
/// </summary>
public sealed class McpToolRegistry : IMcpToolRegistry
{
    private readonly Dictionary<string, IMcpServer> servers;
    private readonly SemaphoreSlim semaphore;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpToolRegistry"/> class.
    /// </summary>
    public McpToolRegistry()
    {
        this.servers = new Dictionary<string, IMcpServer>();
        this.semaphore = new SemaphoreSlim(1, 1);
    }

    /// <inheritdoc/>
    public async Task RegisterAsync(IMcpServer server, CancellationToken cancellationToken = default)
    {
        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
            var id = server.Id;
            if (this.servers.ContainsKey(id))
            {
                throw new InvalidOperationException($"MCP server with ID '{id}' is already registered");
            }

            this.servers[id] = server;
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task UnregisterAsync(string id, CancellationToken cancellationToken = default)
    {
        await this.semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!this.servers.Remove(id))
            {
                throw new InvalidOperationException($"MCP server with ID '{id}' is not registered");
            }
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<IMcpServer> All()
    {
        return this.servers.Values.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<McpTool>> DiscoverToolsAsync(CancellationToken cancellationToken = default)
    {
        var allTools = new List<McpTool>();
        var serverList = this.servers.Values.ToList();

        foreach (var server in serverList)
        {
            var tools = await server.ListToolsAsync(cancellationToken);
            allTools.AddRange(tools);
        }

        return allTools;
    }

    /// <inheritdoc/>
    public async Task<McpTool?> FindToolAsync(string name, CancellationToken cancellationToken = default)
    {
        var serverList = this.servers.Values.ToList();

        foreach (var server in serverList)
        {
            var tools = await server.ListToolsAsync(cancellationToken);
            var tool = tools.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (tool != null)
            {
                return tool;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public bool Validate(McpTool tool, JsonDocument parameters)
    {
        if (tool.Schema == null)
        {
            return true;
        }

        try
        {
            var schemaElement = tool.Schema.RootElement;
            var paramsElement = parameters.RootElement;

            if (schemaElement.TryGetProperty("required", out var requiredProperty) &&
                requiredProperty.ValueKind == JsonValueKind.Array)
            {
                foreach (var requiredField in requiredProperty.EnumerateArray())
                {
                    var fieldName = requiredField.GetString();
                    if (fieldName != null && !paramsElement.TryGetProperty(fieldName, out _))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
