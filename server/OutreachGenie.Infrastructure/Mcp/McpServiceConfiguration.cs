// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutreachGenie.Application.Interfaces.Mcp;
using OutreachGenie.Application.Services;
using OutreachGenie.Application.Services.Mcp;
using OutreachGenie.Domain.Mcp;

namespace OutreachGenie.Infrastructure.Mcp;

/// <summary>
/// Configures MCP server dependencies for dependency injection.
/// </summary>
public static class McpServiceConfiguration
{
    /// <summary>
    /// Registers all MCP servers defined in the specified mcp.json configuration file.
    /// </summary>
    /// <param name="services">Service collection for dependency injection.</param>
    /// <param name="configPath">Absolute path to mcp.json file.</param>
    /// <param name="inputs">Dictionary of input variable values for ${input:var} substitution.</param>
    /// <returns>Service collection for method chaining.</returns>
    public static IServiceCollection AddMcpServersFromConfiguration(
        this IServiceCollection services,
        string configPath,
        Dictionary<string, string>? inputs = null)
    {
        services.AddSingleton<McpConfigurationLoader>();

        var serviceProvider = services.BuildServiceProvider();
        var loader = serviceProvider.GetRequiredService<McpConfigurationLoader>();
        var config = loader.Load(configPath, inputs);

        foreach (var (name, serverConfig) in config.Servers)
        {
            if (serverConfig.Disabled)
            {
                continue;
            }

            if (serverConfig.Type == "stdio" && serverConfig.Command is not null)
            {
                services.AddSingleton<IMcpServer>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<StdioMcpTransport>>();
                    var arguments = string.Join(" ", serverConfig.Args);
                    var transport = new StdioMcpTransport(
                        serverConfig.Command,
                        arguments,
                        logger,
                        serverConfig.Env.Count > 0 ? serverConfig.Env : null);
                    return new GenericMcpServer(name, transport, sp.GetRequiredService<ILogger<GenericMcpServer>>());
                });
            }
            else if (serverConfig.Type == "http" && serverConfig.Url is not null)
            {
                services.AddSingleton<IMcpServer>(sp =>
                {
                    var httpClient = new HttpClient { BaseAddress = new Uri(serverConfig.Url) };
                    var transport = new HttpMcpTransport(httpClient, sp.GetRequiredService<ILogger<HttpMcpTransport>>());
                    return new GenericMcpServer(name, transport, sp.GetRequiredService<ILogger<GenericMcpServer>>());
                });
            }
        }

        return services;
    }

    /// <summary>
    /// Registers Desktop Commander MCP server with stdio transport.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="workingDirectory">Working directory restriction for file operations.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Desktop Commander must be pre-installed to avoid timeout errors during startup.
    /// Run: npx @wonderwhy-er/desktop-commander@latest setup
    /// </remarks>
    public static IServiceCollection AddDesktopCommanderMcpServer(
        this IServiceCollection services,
        string workingDirectory)
    {
        services.AddSingleton<IMcpServer>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<DesktopCommanderMcpServer>>();
            var transportLogger = provider.GetRequiredService<ILogger<StdioMcpTransport>>();

            var transport = new StdioMcpTransport(
                "npx",
                $"@wonderwhy-er/desktop-commander --working-directory \"{workingDirectory}\"",
                transportLogger);

            return new DesktopCommanderMcpServer(transport, logger);
        });

        return services;
    }

    /// <summary>
    /// Registers Playwright MCP server with stdio transport.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="headless">Whether to run browser in headless mode.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPlaywrightMcpServer(
        this IServiceCollection services,
        bool headless = true)
    {
        services.AddSingleton<IMcpServer>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<PlaywrightMcpServer>>();
            var transportLogger = provider.GetRequiredService<ILogger<StdioMcpTransport>>();

            var headlessFlag = headless ? "--headless" : "--headed";
            var transport = new StdioMcpTransport(
                "npx",
                $"-y @playwright/mcp@latest {headlessFlag}",
                transportLogger);

            return new PlaywrightMcpServer(transport, logger);
        });

        return services;
    }

    /// <summary>
    /// Registers Fetch MCP server with stdio transport.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFetchMcpServer(this IServiceCollection services)
    {
        services.AddSingleton<IMcpServer>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<FetchMcpServer>>();
            var transportLogger = provider.GetRequiredService<ILogger<StdioMcpTransport>>();

            var transport = new StdioMcpTransport(
                "npx",
                "-y mcp-fetch-server",
                transportLogger);

            return new FetchMcpServer(transport, logger);
        });

        return services;
    }

    /// <summary>
    /// Registers Exa MCP server with stdio transport.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiKey">Exa API key for authenticated requests.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExaMcpServer(this IServiceCollection services, string apiKey)
    {
        services.AddSingleton<IMcpServer>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ExaMcpServer>>();
            var transportLogger = provider.GetRequiredService<ILogger<StdioMcpTransport>>();

            var environment = new Dictionary<string, string>
            {
                ["EXA_API_KEY"] = apiKey,
            };

            var transport = new StdioMcpTransport(
                "npx",
                "-y exa-mcp-server",
                transportLogger,
                environment);

            return new ExaMcpServer(transport, logger);
        });

        return services;
    }
}
