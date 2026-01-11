namespace OutreachGenie.Domain.Enums;

/// <summary>
/// Represents the engagement state of a lead prospect.
/// </summary>
public enum LeadStatus
{
    /// <summary>
    /// Lead discovered but not yet evaluated.
    /// </summary>
    New,

    /// <summary>
    /// Lead scored and queued for outreach.
    /// </summary>
    Qualified,

    /// <summary>
    /// Connection request sent, awaiting response.
    /// </summary>
    PendingConnection,

    /// <summary>
    /// Connection request accepted.
    /// </summary>
    Connected,

    /// <summary>
    /// Initial message sent, awaiting reply.
    /// </summary>
    MessageSent,

    /// <summary>
    /// Lead replied to message.
    /// </summary>
    Responded,

    /// <summary>
    /// Lead explicitly declined or ignored.
    /// </summary>
    Declined,

    /// <summary>
    /// Lead archived or removed from campaign.
    /// </summary>
    Archived,
}
