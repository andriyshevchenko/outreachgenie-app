namespace OutreachGenie.Domain.Enums;

/// <summary>
/// Categorizes stored artifacts for querying and lifecycle management.
/// </summary>
public enum ArtifactType
{
    /// <summary>
    /// Campaign overview and user-provided context (context.md).
    /// </summary>
    Context,

    /// <summary>
    /// Prospect lists with scoring data.
    /// </summary>
    Leads,

    /// <summary>
    /// Message templates or sent message history.
    /// </summary>
    Messages,

    /// <summary>
    /// Lead scoring configuration and heuristics.
    /// </summary>
    Heuristics,

    /// <summary>
    /// Environment configuration and encrypted secrets.
    /// </summary>
    Environment,

    /// <summary>
    /// Agent-created arbitrary data with dynamic schema.
    /// </summary>
    Arbitrary,
}
