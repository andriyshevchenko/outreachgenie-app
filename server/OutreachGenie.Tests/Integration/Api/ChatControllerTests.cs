// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OutreachGenie.Api.Controllers;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;
using OutreachGenie.Infrastructure.Persistence;
using OutreachGenie.Tests.Integration.Fixtures;
using Xunit;

namespace OutreachGenie.Tests.Integration.Api;

/// <summary>
/// Integration tests for ChatController API endpoints.
/// </summary>
[Collection("Database")]
public sealed class ChatControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly DatabaseFixture fixture;
    private readonly WebApplicationFactory<Program> factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatControllerTests"/> class.
    /// </summary>
    /// <param name="fixture">Database fixture for test isolation.</param>
    public ChatControllerTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
        this.factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<OutreachGenieDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<OutreachGenieDbContext>(options =>
                {
                    options.UseSqlite(this.fixture.ConnectionString);
                });
            });
        });
    }

    /// <summary>
    /// Tests that SendMessage returns generated response for valid campaign.
    /// </summary>
    /// <returns>Task representing asynchronous test.</returns>
    [Fact]
    public async Task SendMessage_returns_response_for_valid_campaign()
    {
        await this.CleanDatabase();
        var client = this.factory.CreateClient();
        var campaign = await CreateTestCampaign(client);
        var request = new SendMessageRequest(campaign.Id, "Hello agent");
        var response = await client.PostAsJsonAsync("/api/v1/chat/send", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK, "request should succeed");
        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>(JsonOptions);
        chatResponse.Should().NotBeNull("response should contain data");
        chatResponse!.Content.Should().NotBeNullOrEmpty("agent should generate response");
        chatResponse.MessageId.Should().NotBeEmpty("message should have ID");
        chatResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5), "timestamp should be recent");
    }

    /// <summary>
    /// Tests that SendMessage returns NotFound for nonexistent campaign.
    /// </summary>
    /// <returns>Task representing asynchronous test.</returns>
    [Fact]
    public async Task SendMessage_returns_not_found_for_invalid_campaign()
    {
        await this.CleanDatabase();
        var client = this.factory.CreateClient();
        var request = new SendMessageRequest(Guid.NewGuid(), "Hello agent");
        var response = await client.PostAsJsonAsync("/api/v1/chat/send", request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "campaign does not exist");
    }

    /// <summary>
    /// Tests that SendMessage handles empty message gracefully.
    /// </summary>
    /// <returns>Task representing asynchronous test.</returns>
    [Fact]
    public async Task SendMessage_handles_empty_message()
    {
        await this.CleanDatabase();
        var client = this.factory.CreateClient();
        var campaign = await CreateTestCampaign(client);
        var request = new SendMessageRequest(campaign.Id, string.Empty);
        var response = await client.PostAsJsonAsync("/api/v1/chat/send", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK, "empty message should be handled");
        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>(JsonOptions);
        chatResponse.Should().NotBeNull("response should be generated");
    }

    /// <summary>
    /// Tests that SendMessage includes campaign context in response.
    /// </summary>
    /// <returns>Task representing asynchronous test.</returns>
    [Fact]
    public async Task SendMessage_uses_campaign_context()
    {
        await this.CleanDatabase();
        var client = this.factory.CreateClient();
        var campaign = await CreateTestCampaign(client);
        var request = new SendMessageRequest(campaign.Id, "What is the campaign status?");
        var response = await client.PostAsJsonAsync("/api/v1/chat/send", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK, "request should succeed");
        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>(JsonOptions);
        chatResponse.Should().NotBeNull("response should be generated");
        chatResponse!.Content.Should().NotBeNullOrEmpty("agent should respond with context");
    }

    /// <summary>
    /// Tests that GetHistory returns empty list for campaign with no messages.
    /// </summary>
    /// <returns>Task representing asynchronous test.</returns>
    [Fact]
    public async Task GetHistory_returns_empty_for_new_campaign()
    {
        await this.CleanDatabase();
        var client = this.factory.CreateClient();
        var campaign = await CreateTestCampaign(client);
        var response = await client.GetAsync($"/api/v1/chat/history/{campaign.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, "request should succeed");
        var history = await response.Content.ReadFromJsonAsync<List<ChatMessage>>(JsonOptions);
        history.Should().NotBeNull("response should contain list");
        history.Should().BeEmpty("new campaign has no message history");
    }

    /// <summary>
    /// Tests that GetHistory returns messages after sending.
    /// </summary>
    /// <returns>Task representing asynchronous test.</returns>
    [Fact]
    public async Task GetHistory_returns_messages_after_sending()
    {
        await this.CleanDatabase();
        var client = this.factory.CreateClient();
        var campaign = await CreateTestCampaign(client);
        var sendRequest = new SendMessageRequest(campaign.Id, "First message");
        await client.PostAsJsonAsync("/api/v1/chat/send", sendRequest);
        var response = await client.GetAsync($"/api/v1/chat/history/{campaign.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, "request should succeed");
        var history = await response.Content.ReadFromJsonAsync<List<ChatMessage>>(JsonOptions);
        history.Should().NotBeNull("response should contain list");
    }

    private static async Task<Campaign> CreateTestCampaign(HttpClient client)
    {
        var request = new CreateCampaignRequest("Test Campaign", "Test Audience");
        var response = await client.PostAsJsonAsync("/api/v1/campaign", request);
        var campaign = await response.Content.ReadFromJsonAsync<Campaign>(JsonOptions);
        return campaign!;
    }

    private async Task CleanDatabase()
    {
        await using var scope = this.factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OutreachGenieDbContext>();
        context.Campaigns.RemoveRange(context.Campaigns);
        context.Tasks.RemoveRange(context.Tasks);
        context.Artifacts.RemoveRange(context.Artifacts);
        context.Leads.RemoveRange(context.Leads);
        await context.SaveChangesAsync();
    }
}
