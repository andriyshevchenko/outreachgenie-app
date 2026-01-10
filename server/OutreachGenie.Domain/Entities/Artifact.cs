using OutreachGenie.Domain.Enums;

namespace OutreachGenie.Domain.Entities;

/// <summary>
/// Represents persistent data created during campaign execution, supporting arbitrary schemas.
/// </summary>
public sealed class Artifact
{
    /// <summary>
    /// Gets or sets the unique identifier for this artifact.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the campaign this artifact belongs to.
    /// </summary>
    public Guid CampaignId { get; set; }

    /// <summary>
    /// Gets or sets the categorization of this artifact.
    /// </summary>
    public ArtifactType Type { get; set; }

    /// <summary>
    /// Gets or sets the stable identifier for querying specific artifacts (e.g., "campaign_context").
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the serialized content of the artifact.
    /// </summary>
    public string ContentJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the origin of this artifact.
    /// </summary>
    public ArtifactSource Source { get; set; }

    /// <summary>
    /// Gets or sets the version number for artifact evolution tracking.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the timestamp when this artifact was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the associated campaign entity.
    /// </summary>
    public Campaign Campaign { get; set; } = null!;
}
