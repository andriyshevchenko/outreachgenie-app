namespace OutreachGenie.Api.Domain.Entities;

/// <summary>
/// Represents the phase of a campaign lifecycle.
/// </summary>
public enum CampaignPhase
{
    /// <summary>Initial planning phase.</summary>
    Planning,

    /// <summary>Lead discovery phase.</summary>
    Discovery,

    /// <summary>Lead scoring phase.</summary>
    Scoring,

    /// <summary>Outreach execution phase.</summary>
    Outreach,

    /// <summary>Results monitoring phase.</summary>
    Monitoring,

    /// <summary>Campaign completed.</summary>
    Complete
}
