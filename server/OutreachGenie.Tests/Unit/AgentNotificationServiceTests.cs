using Microsoft.AspNetCore.SignalR;
using Moq;
using OutreachGenie.Api.Hubs;
using OutreachGenie.Api.Services;
using Xunit;

namespace OutreachGenie.Tests.Unit;

public sealed class AgentNotificationServiceTests
{
    [Fact]
    public async Task NotifyTaskStatusChanged_ShouldBroadcastToAllClients()
    {
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        var mockHubContext = new Mock<IHubContext<AgentHub>>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var service = new AgentNotificationService(mockHubContext.Object);

        await service.NotifyTaskStatusChanged("task-123", "Completed");

        mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "TaskStatusChanged",
                It.Is<object[]>(args =>
                    args.Length == 1 &&
                    args[0] != null),
                default),
            Times.Once);
    }

    [Fact]
    public async Task NotifyChatMessageReceived_ShouldBroadcastToAllClients()
    {
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        var mockHubContext = new Mock<IHubContext<AgentHub>>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var service = new AgentNotificationService(mockHubContext.Object);

        await service.NotifyChatMessageReceived("msg-456", "Hello world", "assistant");

        mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "ChatMessageReceived",
                It.Is<object[]>(args =>
                    args.Length == 1 &&
                    args[0] != null),
                default),
            Times.Once);
    }

    [Fact]
    public async Task NotifyCampaignStateChanged_ShouldBroadcastToAllClients()
    {
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        var mockHubContext = new Mock<IHubContext<AgentHub>>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var service = new AgentNotificationService(mockHubContext.Object);

        await service.NotifyCampaignStateChanged("campaign-789", "Active");

        mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "CampaignStateChanged",
                It.Is<object[]>(args =>
                    args.Length == 1 &&
                    args[0] != null),
                default),
            Times.Once);
    }

    [Fact]
    public async Task NotifyArtifactCreated_ShouldBroadcastToAllClients()
    {
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        var mockHubContext = new Mock<IHubContext<AgentHub>>();
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        var service = new AgentNotificationService(mockHubContext.Object);

        await service.NotifyArtifactCreated("leads", "main", "campaign-111");

        mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "ArtifactCreated",
                It.Is<object[]>(args =>
                    args.Length == 1 &&
                    args[0] != null),
                default),
            Times.Once);
    }
}
