using OutreachGenie.Domain.Enums;

namespace OutreachGenie.Domain.Entities;

/// <summary>
/// Represents a LinkedIn prospect targeted by a campaign with scoring metadata.
/// </summary>
public sealed class Lead
{
    /// <summary>
    /// Gets or sets the unique identifier for this lead.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the campaign this lead belongs to.
    /// </summary>
    public Guid CampaignId { get; set; }

    /// <summary>
    /// Gets or sets the full name of the prospect.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the LinkedIn profile URL.
    /// </summary>
    public string ProfileUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current job title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the profile headline.
    /// </summary>
    public string Headline { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the location information.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the weighted relevance score (0.0 to 1.0).
    /// </summary>
    public double WeightScore { get; set; }

    /// <summary>
    /// Gets or sets the current engagement state.
    /// </summary>
    public LeadStatus Status { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON.
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the lead was discovered.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of last status update.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the associated campaign entity.
    /// </summary>
    public Campaign Campaign { get; set; } = null!;
}
