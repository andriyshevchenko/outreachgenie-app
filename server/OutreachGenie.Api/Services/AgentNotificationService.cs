using Microsoft.AspNetCore.SignalR;
using OutreachGenie.Api.Hubs;

namespace OutreachGenie.Api.Services;

/// <summary>
/// Implementation of IAgentNotificationService using SignalR hub context.
/// </summary>
public sealed class AgentNotificationService : IAgentNotificationService
{
    private readonly IHubContext<AgentHub> hubContext;

    /// <summary>
    /// Initializes notification service with SignalR hub context.
    /// </summary>
    /// <param name="hubContext">SignalR hub context for broadcasting</param>
    public AgentNotificationService(IHubContext<AgentHub> hubContext)
    {
        this.hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task NotifyTaskStatusChanged(string taskId, string newStatus)
    {
        await hubContext.Clients.All.SendAsync("TaskStatusChanged", new
        {
            TaskId = taskId,
            Status = newStatus,
            Timestamp = DateTime.UtcNow,
        });
    }

    /// <inheritdoc />
    public async Task NotifyChatMessageReceived(string messageId, string content, string role)
    {
        await hubContext.Clients.All.SendAsync("ChatMessageReceived", new
        {
            MessageId = messageId,
            Content = content,
            Role = role,
            Timestamp = DateTime.UtcNow,
        });
    }

    /// <inheritdoc />
    public async Task NotifyCampaignStateChanged(string campaignId, string newStatus)
    {
        await hubContext.Clients.All.SendAsync("CampaignStateChanged", new
        {
            CampaignId = campaignId,
            Status = newStatus,
            Timestamp = DateTime.UtcNow,
        });
    }

    /// <inheritdoc />
    public async Task NotifyArtifactCreated(string artifactType, string key, string campaignId)
    {
        await hubContext.Clients.All.SendAsync("ArtifactCreated", new
        {
            Type = artifactType,
            Key = key,
            CampaignId = campaignId,
            Timestamp = DateTime.UtcNow,
        });
    }
}
