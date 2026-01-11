namespace OutreachGenie.Domain.Mcp;

/// <summary>
/// Represents the root configuration for Model Context Protocol servers loaded from mcp.json.
/// </summary>
/// <remarks>
/// This configuration supports dynamic MCP server registration, allowing users to add any MCP
/// server by editing mcp.json without modifying application code. Supports stdio and HTTP transports,
/// environment variables, command-line arguments, and input variable substitution.
/// </remarks>
public sealed class McpConfiguration
{
    /// <summary>
    /// Gets dictionary of MCP server configurations keyed by server name.
    /// </summary>
    public Dictionary<string, McpServerConfig> Servers { get; init; } = new();

    /// <summary>
    /// Gets list of input variable definitions for dynamic configuration values.
    /// </summary>
    public List<McpInput> Inputs { get; init; } = new();
}
