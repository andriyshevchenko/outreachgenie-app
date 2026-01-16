namespace OutreachGenie.Api.Domain.Entities;

/// <summary>
/// Represents a lead discovered for a campaign.
/// </summary>
public sealed class Lead
{
    private Lead()
    {
        // Required for EF Core
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Lead"/> class.
    /// </summary>
    public Lead(
        Guid id,
        Guid campaignId,
        string source,
        string data,
        DateTime createdAt)
    {
        this.Id = id;
        this.CampaignId = campaignId;
        this.Source = source;
        this.Data = data;
        this.CreatedAt = createdAt;
    }

    /// <summary>
    /// Lead identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Campaign identifier.
    /// </summary>
    public Guid CampaignId { get; private set; }

    /// <summary>
    /// Source of the lead.
    /// </summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>
    /// Lead score.
    /// </summary>
    public decimal? Score { get; set; }

    /// <summary>
    /// Scoring rationale.
    /// </summary>
    public string? ScoringRationale { get; set; }

    /// <summary>
    /// Lead data as JSON string.
    /// </summary>
    public string Data { get; private set; } = string.Empty;

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Scoring timestamp.
    /// </summary>
    public DateTime? ScoredAt { get; set; }

    /// <summary>
    /// Parent campaign.
    /// </summary>
    public Campaign Campaign { get; private set; } = null!;
}
