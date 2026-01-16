using OutreachGenie.Api.Domain.Entities;

namespace OutreachGenie.Api.Models;

/// <summary>
/// Campaign data transfer object.
/// </summary>
public sealed class CampaignDto
{
    /// <summary>
    /// Campaign identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Campaign name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current phase.
    /// </summary>
    public string Phase { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Creates DTO from entity.
    /// </summary>
    public static CampaignDto FromEntity(Campaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);

        return new CampaignDto
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Phase = campaign.Phase.ToString(),
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt,
        };
    }
}
