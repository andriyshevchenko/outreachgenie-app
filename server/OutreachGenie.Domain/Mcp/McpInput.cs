namespace OutreachGenie.Domain.Mcp;

/// <summary>
/// Defines an input variable that can be referenced in server configurations.
/// </summary>
public sealed class McpInput
{
    /// <summary>
    /// Gets unique identifier for the input variable (e.g., "ado_org").
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets input type: "promptString" for user-provided strings.
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets description shown to users when prompting for input.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the input should be treated as a password (masked).
    /// </summary>
    public bool Password { get; init; }
}
