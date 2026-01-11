namespace OutreachGenie.Domain.Mcp;

/// <summary>
/// Configuration for a single MCP server instance.
/// </summary>
public sealed class McpServerConfig
{
    /// <summary>
    /// Gets transport type: "stdio" for subprocess communication, "http" for remote servers.
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets command to execute for stdio transport (e.g., "npx", "node", "python").
    /// </summary>
    public string? Command { get; init; }

    /// <summary>
    /// Gets command-line arguments passed to the command.
    /// </summary>
    public List<string> Args { get; init; } = new();

    /// <summary>
    /// Gets URL for HTTP transport servers.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Gets environment variables to set for the MCP server process.
    /// </summary>
    public Dictionary<string, string> Env { get; init; } = new();

    /// <summary>
    /// Gets a value indicating whether the server is disabled and should not be registered.
    /// </summary>
    public bool Disabled { get; init; }

    /// <summary>
    /// Gets list of tool names that should be auto-approved without user confirmation.
    /// </summary>
    public List<string> AutoApprove { get; init; } = new();
}
