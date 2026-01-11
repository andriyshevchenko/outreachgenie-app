namespace OutreachGenie.Api.Services;

/// <summary>
/// Service for broadcasting real-time events via SignalR.
/// Used by controllers and background services to notify clients of state changes.
/// </summary>
public interface IAgentNotificationService
{
    /// <summary>
    /// Notifies all clients that a task status has changed.
    /// </summary>
    Task NotifyTaskStatusChanged(string taskId, string newStatus);

    /// <summary>
    /// Notifies all clients that a chat message was received.
    /// </summary>
    Task NotifyChatMessageReceived(string messageId, string content, string role);

    /// <summary>
    /// Notifies all clients that a campaign state has changed.
    /// </summary>
    Task NotifyCampaignStateChanged(string campaignId, string newStatus);

    /// <summary>
    /// Notifies all clients that an artifact was created.
    /// </summary>
    Task NotifyArtifactCreated(string artifactType, string key, string campaignId);
}
