namespace OutreachGenie.Application.Services;

/// <summary>
/// Represents an action proposal from LLM.
/// </summary>
public sealed class ActionProposal
{
    /// <summary>
    /// Gets or sets action type.
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets task identifier.
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// Gets or sets action parameters as JSON.
    /// </summary>
    public string? Parameters { get; set; }
}
