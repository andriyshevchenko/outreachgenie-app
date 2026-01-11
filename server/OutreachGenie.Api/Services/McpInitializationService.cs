// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using OutreachGenie.Application.Services.Mcp;
using OutreachGenie.Infrastructure.Mcp;

namespace OutreachGenie.Api.Services;

/// <summary>
/// Background service that initializes and registers MCP servers on application startup.
/// Connects to Desktop Commander, Playwright, Fetch, and Exa MCP servers.
/// </summary>
public sealed class McpInitializationService : IHostedService
{
    private readonly IMcpToolRegistry registry;
    private readonly ILogger<McpInitializationService> logger;
    private readonly ILoggerFactory loggerFactory;
    private readonly List<IMcpServer> servers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="McpInitializationService"/> class.
    /// </summary>
    /// <param name="registry">MCP tool registry for server registration.</param>
    /// <param name="logger">Logger for initialization operations.</param>
    /// <param name="loggerFactory">Logger factory for creating server loggers.</param>
    public McpInitializationService(
        IMcpToolRegistry registry,
        ILogger<McpInitializationService> logger,
        ILoggerFactory loggerFactory)
    {
        this.registry = registry;
        this.logger = logger;
        this.loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Starts MCP servers and registers them with the tool registry.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the initialization operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(
            async () =>
            {
                this.logger.LogInformation("Initializing MCP servers in background");
                try
                {
                    await InitializeDesktopCommanderAsync(default);
                    await InitializePlaywrightAsync(default);
                    this.logger.LogInformation("MCP servers initialized successfully. {ServerCount} servers registered", this.servers.Count);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to initialize MCP servers");
                }
            },
            cancellationToken);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops and disconnects all MCP servers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the shutdown operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Stopping MCP servers");

        foreach (var server in this.servers)
        {
            try
            {
                await server.DisconnectAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Error disconnecting MCP server {ServerName}", server.Name);
            }
        }

        this.servers.Clear();
    }

    private async Task InitializeDesktopCommanderAsync(CancellationToken cancellationToken)
    {
        var transport = new StdioMcpTransport(
            "npx",
            "-y @wonderwhy-er/desktop-commander",
            this.loggerFactory.CreateLogger<StdioMcpTransport>());

        var server = new DesktopCommanderMcpServer(
            transport,
            this.loggerFactory.CreateLogger<DesktopCommanderMcpServer>());

        await server.ConnectAsync(cancellationToken);
        await this.registry.RegisterAsync(server, cancellationToken);
        this.servers.Add(server);
        this.logger.LogInformation("Desktop Commander MCP server registered");
    }

    private async Task InitializePlaywrightAsync(CancellationToken cancellationToken)
    {
        var transport = new StdioMcpTransport(
            "npx",
            "-y @playwright/mcp@latest",
            this.loggerFactory.CreateLogger<StdioMcpTransport>());

        var server = new PlaywrightMcpServer(
            transport,
            this.loggerFactory.CreateLogger<PlaywrightMcpServer>());

        await server.ConnectAsync(cancellationToken);
        await this.registry.RegisterAsync(server, cancellationToken);
        this.servers.Add(server);
        this.logger.LogInformation("Playwright MCP server registered");
    }
}
