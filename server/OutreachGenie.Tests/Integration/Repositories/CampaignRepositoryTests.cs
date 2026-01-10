using FluentAssertions;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;
using OutreachGenie.Infrastructure.Persistence.Repositories;
using OutreachGenie.Tests.Integration.Fixtures;
using Xunit;

namespace OutreachGenie.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for CampaignRepository using Testcontainers.
/// </summary>
[Collection("Database")]
public sealed class CampaignRepositoryTests
{
    private readonly DatabaseFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="CampaignRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The database fixture.</param>
    public CampaignRepositoryTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Tests that CreateAsync successfully creates a campaign.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CreateAsync_ShouldCreateCampaign()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new CampaignRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Test Campaign",
            Status = CampaignStatus.Initializing,
            TargetAudience = "Software Engineers",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/path",
        };

        var result = await repository.CreateAsync(campaign);

        result.Should().NotBeNull();
        result.Id.Should().Be(campaign.Id);
        result.Name.Should().Be("Test Campaign");
    }

    /// <summary>
    /// Tests that GetByIdAsync retrieves correct campaign.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetByIdAsync_ShouldReturnCampaign_WhenExists()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new CampaignRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Retrievable Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "DevOps Engineers",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/retrieve",
        };
        await repository.CreateAsync(campaign);

        var retrieved = await repository.GetByIdAsync(campaign.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(campaign.Id);
        retrieved.Name.Should().Be("Retrievable Campaign");
        retrieved.Status.Should().Be(CampaignStatus.Active);
    }

    /// <summary>
    /// Tests that GetByIdAsync returns null for non-existent campaign.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new CampaignRepository(context);
        var nonExistentId = Guid.NewGuid();

        var result = await repository.GetByIdAsync(nonExistentId);

        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetWithAllRelatedAsync includes tasks, artifacts, and leads.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetWithAllRelatedAsync_ShouldIncludeRelatedEntities()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new CampaignRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Campaign with Relations",
            Status = CampaignStatus.Active,
            TargetAudience = "CTOs",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/relations",
        };
        campaign.Tasks.Add(new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Description = "Test Task",
            Status = Domain.Enums.TaskStatus.Pending,
            Type = "search",
            CreatedAt = DateTime.UtcNow,
        });
        campaign.Artifacts.Add(new Artifact
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Type = ArtifactType.Context,
            ContentJson = "{\"test\":\"data\"}",
            Source = ArtifactSource.User,
            CreatedAt = DateTime.UtcNow,
        });
        campaign.Leads.Add(new Lead
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            FullName = "John Doe",
            ProfileUrl = "https://linkedin.com/in/johndoe",
            Title = "CTO",
            Headline = "Tech Leader",
            Location = "San Francisco",
            WeightScore = 0.95,
            Status = LeadStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await repository.CreateAsync(campaign);

        var retrieved = await repository.GetWithAllRelatedAsync(campaign.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Tasks.Should().HaveCount(1);
        retrieved.Artifacts.Should().HaveCount(1);
        retrieved.Leads.Should().HaveCount(1);
        retrieved.Leads.Should().ContainSingle(l => l.FullName == "John Doe");
    }

    /// <summary>
    /// Tests that UpdateAsync modifies existing campaign.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task UpdateAsync_ShouldModifyCampaign()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new CampaignRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            Status = CampaignStatus.Active,
            TargetAudience = "Developers",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/update",
        };
        await repository.CreateAsync(campaign);
        campaign.Name = "Updated Name";
        campaign.Status = CampaignStatus.Paused;

        await repository.UpdateAsync(campaign);
        var updated = await repository.GetByIdAsync(campaign.Id);

        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.Status.Should().Be(CampaignStatus.Paused);
    }

    /// <summary>
    /// Tests that DeleteAsync removes campaign.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DeleteAsync_ShouldRemoveCampaign()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new CampaignRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "To Delete",
            Status = CampaignStatus.Completed,
            TargetAudience = "Testers",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/delete",
        };
        await repository.CreateAsync(campaign);

        await repository.DeleteAsync(campaign.Id);
        var deleted = await repository.GetByIdAsync(campaign.Id);

        deleted.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetAllAsync returns all campaigns.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCampaigns()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new CampaignRepository(context);
        var campaign1 = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Campaign 1",
            Status = CampaignStatus.Active,
            TargetAudience = "Group 1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/all1",
        };
        var campaign2 = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Campaign 2",
            Status = CampaignStatus.Paused,
            TargetAudience = "Group 2",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/all2",
        };
        await repository.CreateAsync(campaign1);
        await repository.CreateAsync(campaign2);

        var all = await repository.GetAllAsync();

        all.Should().HaveCountGreaterOrEqualTo(2);
        all.Should().Contain(c => c.Id == campaign1.Id);
        all.Should().Contain(c => c.Id == campaign2.Id);
    }
}
