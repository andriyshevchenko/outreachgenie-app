// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Net;
using System.Net.Http.Json;
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
/// Integration tests for CampaignController API endpoints using shared Testcontainer.
/// </summary>
[Collection("Database")]
public sealed class CampaignControllerTests
{
    private readonly DatabaseFixture fixture;
    private readonly WebApplicationFactory<Program> factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CampaignControllerTests"/> class.
    /// </summary>
    /// <param name="fixture">The database fixture.</param>
    public CampaignControllerTests(DatabaseFixture fixture)
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
    /// Tests that Create endpoint returns created campaign.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Create_returns_created_campaign()
    {
        await CleanDatabase();
        var client = this.factory.CreateClient();
        var request = new CreateCampaignRequest(
            "Test Campaign",
            "Software engineers in tech companies");
        var response = await client.PostAsJsonAsync("/api/v1/campaign", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created, "campaign should be created");
        var campaign = await response.Content.ReadFromJsonAsync<Campaign>();
        campaign.Should().NotBeNull("response should contain campaign");
        campaign!.Name.Should().Be("Test Campaign", "name should match request");
        campaign.TargetAudience.Should().Be(
            "Software engineers in tech companies",
            "target audience should match request");
        campaign.Status.Should().Be(CampaignStatus.Initializing, "new campaigns start initializing");
        response.Headers.Location.Should().NotBeNull("Location header should be set");
    }

    /// <summary>
    /// Tests that Get endpoint returns existing campaign.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Get_returns_existing_campaign()
    {
        await CleanDatabase();
        var client = this.factory.CreateClient();
        var createRequest = new CreateCampaignRequest("Get Test", "Target audience");
        var createResponse = await client.PostAsJsonAsync("/api/v1/campaign", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Campaign>();
        var getResponse = await client.GetAsync($"/api/v1/campaign/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK, "campaign should exist");
        var retrieved = await getResponse.Content.ReadFromJsonAsync<Campaign>();
        retrieved.Should().NotBeNull("response should contain campaign");
        retrieved!.Id.Should().Be(created.Id, "ID should match");
        retrieved.Name.Should().Be("Get Test", "name should match");
    }

    /// <summary>
    /// Tests that Get endpoint returns not found for missing campaign.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Get_returns_not_found_for_missing_campaign()
    {
        await CleanDatabase();
        var client = this.factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/campaign/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "campaign does not exist");
    }

    /// <summary>
    /// Tests that List endpoint returns all campaigns.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task List_returns_all_campaigns()
    {
        await CleanDatabase();
        var client = this.factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/v1/campaign",
            new CreateCampaignRequest("Campaign 1", "Audience 1"));
        await client.PostAsJsonAsync(
            "/api/v1/campaign",
            new CreateCampaignRequest("Campaign 2", "Audience 2"));
        var response = await client.GetAsync("/api/v1/campaign");
        response.StatusCode.Should().Be(HttpStatusCode.OK, "list should succeed");
        var campaigns = await response.Content.ReadFromJsonAsync<List<Campaign>>();
        campaigns.Should().NotBeNull("response should contain campaigns");
        campaigns!.Should().HaveCountGreaterOrEqualTo(2, "at least two campaigns should exist");
    }

    /// <summary>
    /// Tests that Pause endpoint transitions active campaign to paused.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Pause_transitions_active_campaign_to_paused()
    {
        await CleanDatabase();
        var client = this.factory.CreateClient();
        var campaign = await CreateActiveCampaign(client);
        var response = await client.PostAsync($"/api/v1/campaign/{campaign.Id}/pause", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "pause should succeed");
        var getResponse = await client.GetAsync($"/api/v1/campaign/{campaign.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<Campaign>();
        updated!.Status.Should().Be(CampaignStatus.Paused, "campaign should be paused");
    }

    /// <summary>
    /// Tests that Pause endpoint returns bad request for non active campaign.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Pause_returns_bad_request_for_non_active_campaign()
    {
        await CleanDatabase();
        var client = this.factory.CreateClient();
        var createRequest = new CreateCampaignRequest("Test", "Audience");
        var createResponse = await client.PostAsJsonAsync("/api/v1/campaign", createRequest);
        var campaign = await createResponse.Content.ReadFromJsonAsync<Campaign>();
        var response = await client.PostAsync($"/api/v1/campaign/{campaign!.Id}/pause", null);
        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            "cannot pause non-active campaign");
    }

    /// <summary>
    /// Tests that Resume endpoint transitions paused campaign to active.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Resume_transitions_paused_campaign_to_active()
    {
        await CleanDatabase();
        var client = this.factory.CreateClient();
        var campaign = await CreatePausedCampaign(client);
        var response = await client.PostAsync($"/api/v1/campaign/{campaign.Id}/resume", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "resume should succeed");
        var getResponse = await client.GetAsync($"/api/v1/campaign/{campaign.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<Campaign>();
        updated!.Status.Should().Be(CampaignStatus.Active, "campaign should be active");
    }

    /// <summary>
    /// Tests that Resume endpoint returns bad request for non paused campaign.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Resume_returns_bad_request_for_non_paused_campaign()
    {
        await CleanDatabase();
        var client = this.factory.CreateClient();
        var campaign = await CreateActiveCampaign(client);
        var response = await client.PostAsync($"/api/v1/campaign/{campaign.Id}/resume", null);
        response.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            "cannot resume non-paused campaign");
    }

    /// <summary>
    /// Tests that Delete endpoint removes campaign.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Delete_removes_campaign()
    {
        await CleanDatabase();
        var client = this.factory.CreateClient();
        var createRequest = new CreateCampaignRequest("To Delete", "Audience");
        var createResponse = await client.PostAsJsonAsync("/api/v1/campaign", createRequest);
        var campaign = await createResponse.Content.ReadFromJsonAsync<Campaign>();
        var deleteResponse = await client.DeleteAsync($"/api/v1/campaign/{campaign!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, "delete should succeed");
        var getResponse = await client.GetAsync($"/api/v1/campaign/{campaign.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, "campaign should be deleted");
    }

    /// <summary>
    /// Tests that Delete endpoint returns not found for missing campaign.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task Delete_returns_not_found_for_missing_campaign()
    {
        await CleanDatabase();
        var client = this.factory.CreateClient();
        var response = await client.DeleteAsync($"/api/v1/campaign/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "campaign does not exist");
    }

    private async Task<Campaign> CreateActiveCampaign(HttpClient client)
    {
        var createRequest = new CreateCampaignRequest("Active Campaign", "Target");
        var createResponse = await client.PostAsJsonAsync("/api/v1/campaign", createRequest);
        var campaign = await createResponse.Content.ReadFromJsonAsync<Campaign>();
        using var scope = this.factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutreachGenieDbContext>();
        var entity = await db.Campaigns.FindAsync(campaign!.Id);
        entity!.Status = CampaignStatus.Active;
        await db.SaveChangesAsync();
        return campaign;
    }

    private async Task<Campaign> CreatePausedCampaign(HttpClient client)
    {
        var campaign = await CreateActiveCampaign(client);
        await client.PostAsync($"/api/v1/campaign/{campaign.Id}/pause", null);
        var getResponse = await client.GetAsync($"/api/v1/campaign/{campaign.Id}");
        return (await getResponse.Content.ReadFromJsonAsync<Campaign>())!;
    }

    private async Task CleanDatabase()
    {
        await using var context = this.fixture.CreateDbContext();
        context.RemoveRange(context.Artifacts);
        context.RemoveRange(context.Leads);
        context.RemoveRange(context.Tasks);
        context.RemoveRange(context.Campaigns);
        await context.SaveChangesAsync();
    }
}
