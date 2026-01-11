using Microsoft.AspNetCore.SignalR;

namespace OutreachGenie.Api.Hubs;

/// <summary>
/// SignalR hub for real-time agent updates.
/// Broadcasts task status changes, chat messages, campaign state changes, and artifact creation events to connected clients.
/// </summary>
public sealed class AgentHub : Hub
{
    /// <summary>
    /// Broadcasts task status change to all clients.
    /// </summary>
    /// <param name="taskId">Task identifier</param>
    /// <param name="newStatus">New task status</param>
    public async Task TaskStatusChanged(string taskId, string newStatus)
    {
        await Clients.All.SendAsync("TaskStatusChanged", new
        {
            TaskId = taskId,
            Status = newStatus,
            Timestamp = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Broadcasts chat message to all clients.
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="content">Message content</param>
    /// <param name="role">Message role (user or assistant)</param>
    public async Task ChatMessageReceived(string messageId, string content, string role)
    {
        await Clients.All.SendAsync("ChatMessageReceived", new
        {
            MessageId = messageId,
            Content = content,
            Role = role,
            Timestamp = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Broadcasts campaign state change to all clients.
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    /// <param name="newStatus">New campaign status</param>
    public async Task CampaignStateChanged(string campaignId, string newStatus)
    {
        await Clients.All.SendAsync("CampaignStateChanged", new
        {
            CampaignId = campaignId,
            Status = newStatus,
            Timestamp = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Broadcasts artifact creation to all clients.
    /// </summary>
    /// <param name="artifactType">Type of artifact created</param>
    /// <param name="key">Artifact key</param>
    /// <param name="campaignId">Campaign identifier</param>
    public async Task ArtifactCreated(string artifactType, string key, string campaignId)
    {
        await Clients.All.SendAsync("ArtifactCreated", new
        {
            Type = artifactType,
            Key = key,
            CampaignId = campaignId,
            Timestamp = DateTime.UtcNow,
        });
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        Console.WriteLine($"Client connected: {Context.ConnectionId}");
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
    }
}
