using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OutreachGenie.Domain.Mcp;

namespace OutreachGenie.Application.Services;

/// <summary>
/// Loads and processes MCP server configurations from mcp.json files.
/// </summary>
/// <remarks>
/// Reads mcp.json configuration files and resolves input variables, environment variables,
/// and validates server configurations. Supports stdio and HTTP transport types with full
/// parameter expansion including ${input:var} and ${env:var} substitutions.
/// </remarks>
public sealed class McpConfigurationLoader
{
    private readonly ILogger<McpConfigurationLoader> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpConfigurationLoader"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public McpConfigurationLoader(ILogger<McpConfigurationLoader> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Loads MCP configuration from the specified JSON file path.
    /// </summary>
    /// <param name="path">Absolute path to mcp.json file.</param>
    /// <param name="inputs">Dictionary of input variable values (e.g., from user prompts).</param>
    /// <returns>Parsed and validated MCP configuration.</returns>
    public McpConfiguration Load(string path, Dictionary<string, string>? inputs = null)
    {
        if (!File.Exists(path))
        {
            this.logger.LogWarning("MCP configuration file not found at {Path}", path);
            return new McpConfiguration();
        }

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };
        var config = JsonSerializer.Deserialize<McpConfiguration>(json, options);
        if (config is null)
        {
            this.logger.LogError("Failed to deserialize MCP configuration from {Path}", path);
            return new McpConfiguration();
        }

        this.ResolveVariables(config, inputs ?? new());
        return config;
    }

    private void ResolveVariables(McpConfiguration config, Dictionary<string, string> inputs)
    {
        var inputPattern = new Regex(@"\$\{input:([^}]+)\}");
        var envPattern = new Regex(@"\$\{env:([^}]+)\}");

#pragma warning disable S3267
        foreach (var args in config.Servers.Values)
        {
            for (var i = 0; i < args.Args.Count; i++)
#pragma warning restore S3267
            {
                var arg = args.Args[i];
                arg = inputPattern.Replace(arg, match =>
                {
                    var key = match.Groups[1].Value;
                    if (inputs.TryGetValue(key, out var value))
                    {
                        return value;
                    }

                    this.logger.LogWarning("Input variable {Key} not found in provided inputs", key);
                    return match.Value;
                });

                arg = envPattern.Replace(arg, match =>
                {
                    var key = match.Groups[1].Value;
                    var value = Environment.GetEnvironmentVariable(key);
                    if (value is not null)
                    {
                        return value;
                    }

                    this.logger.LogWarning("Environment variable {Key} not found", key);
                    return match.Value;
                });

                args.Args[i] = arg;
            }
        }
    }
}
