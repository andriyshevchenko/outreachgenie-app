using FluentAssertions;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;
using OutreachGenie.Infrastructure.Persistence.Repositories;
using OutreachGenie.Tests.Integration.Fixtures;
using Xunit;

namespace OutreachGenie.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for ArtifactRepository.
/// </summary>
[Collection("Database")]
public sealed class ArtifactRepositoryTests
{
    private readonly DatabaseFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtifactRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The database fixture.</param>
    public ArtifactRepositoryTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Tests that GetByKeyAsync retrieves artifact by campaign, type, and key.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetByKeyAsync_ShouldReturnLatestVersion_WhenMultipleVersionsExist()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new ArtifactRepository(context);
        var campaignRepository = new CampaignRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Test Campaign for Artifacts",
            TargetAudience = "LinkedIn Users",
            Status = CampaignStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/artifacts",
        };
        await campaignRepository.CreateAsync(campaign);
        var artifact1 = new Artifact
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Type = ArtifactType.Heuristics,
            Key = "scoring_config",
            ContentJson = "{\"version\":1}",
            Source = ArtifactSource.User,
            Version = 1,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
        };
        var artifact2 = new Artifact
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Type = ArtifactType.Heuristics,
            Key = "scoring_config",
            ContentJson = "{\"version\":2}",
            Source = ArtifactSource.Agent,
            Version = 2,
            CreatedAt = DateTime.UtcNow,
        };
        await repository.CreateAsync(artifact1);
        await repository.CreateAsync(artifact2);

        var result = await repository.GetByKeyAsync(campaign.Id, ArtifactType.Heuristics, "scoring_config");

        result.Should().NotBeNull();
        result!.Version.Should().Be(2);
        result.ContentJson.Should().Contain("\"version\":2");
    }

    /// <summary>
    /// Tests that GetByTypeAsync filters artifacts by type.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetByTypeAsync_ShouldReturnOnlyMatchingType()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new ArtifactRepository(context);
        var campaignRepository = new CampaignRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Test Campaign for Type Filtering",
            TargetAudience = "Marketing Teams",
            Status = CampaignStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/types",
        };
        await campaignRepository.CreateAsync(campaign);
        var contextArtifact = new Artifact
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Type = ArtifactType.Context,
            ContentJson = "{\"context\":\"data\"}",
            Source = ArtifactSource.User,
            CreatedAt = DateTime.UtcNow,
        };
        var leadsArtifact = new Artifact
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Type = ArtifactType.Leads,
            ContentJson = "{\"leads\":[]}",
            Source = ArtifactSource.Agent,
            CreatedAt = DateTime.UtcNow,
        };
        await repository.CreateAsync(contextArtifact);
        await repository.CreateAsync(leadsArtifact);

        var results = await repository.GetByTypeAsync(campaign.Id, ArtifactType.Context);

        results.Should().HaveCount(1);
        results[0].Type.Should().Be(ArtifactType.Context);
    }

    /// <summary>
    /// Tests that arbitrary artifacts can be created and retrieved.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CreateAsync_ShouldSupportArbitraryArtifacts()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new ArtifactRepository(context);
        var campaignRepository = new CampaignRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Test Campaign for Arbitrary",
            TargetAudience = "Developers",
            Status = CampaignStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/arbitrary",
        };
        await campaignRepository.CreateAsync(campaign);
        var artifact = new Artifact
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Type = ArtifactType.Arbitrary,
            Key = "custom_data",
            ContentJson = "{\"customField\":\"customValue\",\"nested\":{\"data\":123}}",
            Source = ArtifactSource.Agent,
            CreatedAt = DateTime.UtcNow,
        };

        var created = await repository.CreateAsync(artifact);
        var retrieved = await repository.GetByIdAsync(created.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Type.Should().Be(ArtifactType.Arbitrary);
        retrieved.ContentJson.Should().Contain("customField");
        retrieved.ContentJson.Should().Contain("nested");
    }
}
