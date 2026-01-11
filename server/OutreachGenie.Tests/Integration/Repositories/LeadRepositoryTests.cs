using FluentAssertions;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;
using OutreachGenie.Infrastructure.Persistence.Repositories;
using OutreachGenie.Tests.Integration.Fixtures;
using Xunit;

namespace OutreachGenie.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for LeadRepository.
/// </summary>
[Collection("Database")]
public sealed class LeadRepositoryTests
{
    private readonly DatabaseFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeadRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The database fixture.</param>
    public LeadRepositoryTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Tests that GetByCampaignIdAsync returns leads sorted by weight score descending.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetByCampaignIdAsync_ShouldSortByWeightScoreDescending()
    {
        await using var context = this.fixture.CreateDbContext();
        var campaignRepository = new CampaignRepository(context);
        var leadRepository = new LeadRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Lead Test Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "Tech Leads",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/leads",
        };
        await campaignRepository.CreateAsync(campaign);
        var lead1 = new Lead
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            FullName = "Low Score",
            ProfileUrl = "https://linkedin.com/low",
            Title = "Developer",
            Headline = "Software Developer",
            Location = "NYC",
            WeightScore = 0.3,
            Status = LeadStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var lead2 = new Lead
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            FullName = "High Score",
            ProfileUrl = "https://linkedin.com/high",
            Title = "CTO",
            Headline = "Chief Technology Officer",
            Location = "SF",
            WeightScore = 0.95,
            Status = LeadStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var lead3 = new Lead
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            FullName = "Mid Score",
            ProfileUrl = "https://linkedin.com/mid",
            Title = "Engineering Manager",
            Headline = "Tech Lead",
            Location = "Austin",
            WeightScore = 0.65,
            Status = LeadStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        await leadRepository.CreateAsync(lead1);
        await leadRepository.CreateAsync(lead2);
        await leadRepository.CreateAsync(lead3);

        var results = await leadRepository.GetByCampaignIdAsync(campaign.Id);

        results.Should().HaveCountGreaterOrEqualTo(3);
        var campaignLeads = results.Where(l => l.CampaignId == campaign.Id).ToList();
        campaignLeads[0].WeightScore.Should().Be(0.95);
        campaignLeads[1].WeightScore.Should().Be(0.65);
        campaignLeads[2].WeightScore.Should().Be(0.3);
    }

    /// <summary>
    /// Tests that GetTopLeadsAsync returns correct number of top-ranked leads.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetTopLeadsAsync_ShouldReturnTopNLeads()
    {
        await using var context = this.fixture.CreateDbContext();
        var campaignRepository = new CampaignRepository(context);
        var leadRepository = new LeadRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Top Leads Test Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "Directors",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/topLeads",
        };
        await campaignRepository.CreateAsync(campaign);
        for (int i = 0; i < 10; i++)
        {
            await leadRepository.CreateAsync(new Lead
            {
                Id = Guid.NewGuid(),
                CampaignId = campaign.Id,
                FullName = $"Lead {i}",
                ProfileUrl = $"https://linkedin.com/lead{i}",
                Title = "Engineer",
                Headline = "Software Engineer",
                Location = "Remote",
                WeightScore = 0.1 * (10 - i),
                Status = LeadStatus.New,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        var topLeads = await leadRepository.GetTopLeadsAsync(campaign.Id, 3);

        topLeads.Should().HaveCount(3);
        topLeads[0].WeightScore.Should().BeGreaterOrEqualTo(topLeads[1].WeightScore);
        topLeads[1].WeightScore.Should().BeGreaterOrEqualTo(topLeads[2].WeightScore);
    }

    /// <summary>
    /// Tests that CreateBatchAsync inserts multiple leads efficiently.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CreateBatchAsync_ShouldInsertMultipleLeads()
    {
        await using var context = this.fixture.CreateDbContext();
        var campaignRepository = new CampaignRepository(context);
        var leadRepository = new LeadRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Batch Test Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "Analysts",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/batch",
        };
        await campaignRepository.CreateAsync(campaign);
        var leads = new List<Lead>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CampaignId = campaign.Id,
                FullName = "Batch Lead 1",
                ProfileUrl = "https://linkedin.com/batch1",
                Title = "Developer",
                Headline = "Full Stack Developer",
                Location = "LA",
                WeightScore = 0.7,
                Status = LeadStatus.New,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.NewGuid(),
                CampaignId = campaign.Id,
                FullName = "Batch Lead 2",
                ProfileUrl = "https://linkedin.com/batch2",
                Title = "Designer",
                Headline = "UX Designer",
                Location = "Seattle",
                WeightScore = 0.6,
                Status = LeadStatus.New,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };

        await leadRepository.CreateBatchAsync(leads);
        var retrieved = await leadRepository.GetByCampaignIdAsync(campaign.Id);

        retrieved.Should().HaveCountGreaterOrEqualTo(2);
    }

    /// <summary>
    /// Tests that GetByStatusAsync filters leads by status.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetByStatusAsync_ShouldFilterByStatus()
    {
        await using var context = this.fixture.CreateDbContext();
        var campaignRepository = new CampaignRepository(context);
        var leadRepository = new LeadRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Status Filter Test Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "VPs",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/status",
        };
        await campaignRepository.CreateAsync(campaign);
        var newLead = new Lead
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            FullName = "New Lead",
            ProfileUrl = "https://linkedin.com/new",
            Title = "Engineer",
            Headline = "Software Engineer",
            Location = "Boston",
            WeightScore = 0.8,
            Status = LeadStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var qualifiedLead = new Lead
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            FullName = "Qualified Lead",
            ProfileUrl = "https://linkedin.com/qualified",
            Title = "Senior Engineer",
            Headline = "Senior Software Engineer",
            Location = "Chicago",
            WeightScore = 0.85,
            Status = LeadStatus.Qualified,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        await leadRepository.CreateAsync(newLead);
        await leadRepository.CreateAsync(qualifiedLead);

        var newLeads = await leadRepository.GetByStatusAsync(campaign.Id, LeadStatus.New);
        var qualifiedLeads = await leadRepository.GetByStatusAsync(campaign.Id, LeadStatus.Qualified);

        newLeads.Should().HaveCountGreaterOrEqualTo(1);
        newLeads.Should().Contain(l => l.Status == LeadStatus.New);
        qualifiedLeads.Should().HaveCountGreaterOrEqualTo(1);
        qualifiedLeads.Should().Contain(l => l.Status == LeadStatus.Qualified);
    }
}
