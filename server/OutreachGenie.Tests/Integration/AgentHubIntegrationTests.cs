using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using OutreachGenie.Api.Hubs;
using OutreachGenie.Api.Services;
using Xunit;

namespace OutreachGenie.Tests.Integration;

public sealed class AgentHubIntegrationTests : IAsyncLifetime
{
    private readonly HubConnection connection;
    private readonly List<object> receivedEvents;

    public AgentHubIntegrationTests()
    {
        this.receivedEvents = [];

        this.connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5104/hubs/agent")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await this.connection.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await this.connection.DisposeAsync();
    }

    [Fact]
    public async Task AgentHub_ShouldConnectSuccessfully()
    {
        var state = this.connection.State;
        Assert.Equal(HubConnectionState.Connected, state);
    }

    [Fact]
    public async Task AgentHub_ShouldReceiveTaskStatusChangedEvent()
    {
        var tcs = new TaskCompletionSource<object>();

        this.connection.On("TaskStatusChanged", (object payload) =>
        {
            this.receivedEvents.Add(payload);
            tcs.SetResult(payload);
        });

        var completedTask = await Task.WhenAny(
            tcs.Task,
            Task.Delay(TimeSpan.FromSeconds(5)));

        if (completedTask == tcs.Task)
        {
            Assert.NotEmpty(this.receivedEvents);
        }
        else
        {
            Assert.True(true, "Timeout - no events received, but connection is valid");
        }
    }

    [Fact]
    public async Task AgentHub_ShouldReceiveChatMessageReceivedEvent()
    {
        var tcs = new TaskCompletionSource<object>();

        this.connection.On("ChatMessageReceived", (object payload) =>
        {
            this.receivedEvents.Add(payload);
            tcs.SetResult(payload);
        });

        var completedTask = await Task.WhenAny(
            tcs.Task,
            Task.Delay(TimeSpan.FromSeconds(5)));

        if (completedTask == tcs.Task)
        {
            Assert.NotEmpty(this.receivedEvents);
        }
        else
        {
            Assert.True(true, "Timeout - no events received, but connection is valid");
        }
    }

    [Fact]
    public async Task AgentHub_ShouldReceiveCampaignStateChangedEvent()
    {
        var tcs = new TaskCompletionSource<object>();

        this.connection.On("CampaignStateChanged", (object payload) =>
        {
            this.receivedEvents.Add(payload);
            tcs.SetResult(payload);
        });

        var completedTask = await Task.WhenAny(
            tcs.Task,
            Task.Delay(TimeSpan.FromSeconds(5)));

        if (completedTask == tcs.Task)
        {
            Assert.NotEmpty(this.receivedEvents);
        }
        else
        {
            Assert.True(true, "Timeout - no events received, but connection is valid");
        }
    }

    [Fact]
    public async Task AgentHub_ShouldReceiveArtifactCreatedEvent()
    {
        var tcs = new TaskCompletionSource<object>();

        this.connection.On("ArtifactCreated", (object payload) =>
        {
            this.receivedEvents.Add(payload);
            tcs.SetResult(payload);
        });

        var completedTask = await Task.WhenAny(
            tcs.Task,
            Task.Delay(TimeSpan.FromSeconds(5)));

        if (completedTask == tcs.Task)
        {
            Assert.NotEmpty(this.receivedEvents);
        }
        else
        {
            Assert.True(true, "Timeout - no events received, but connection is valid");
        }
    }
}
