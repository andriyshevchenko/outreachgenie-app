// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json;
using FluentAssertions;
using OutreachGenie.Application.Services.LeadScoring;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;
using Xunit;

namespace OutreachGenie.Tests.Unit.Services;

/// <summary>
/// Tests for lead scoring service.
/// </summary>
public sealed class LeadScoringServiceTests
{
    [Fact]
    public void Calculate_returns_zero_for_no_keyword_matches()
    {
        var service = new LeadScoringService();
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            CampaignId = Guid.NewGuid(),
            FullName = "John Doe",
            ProfileUrl = "https://linkedin.com/in/johndoe",
            Title = "Software Engineer",
            Headline = "Building scalable systems",
            Location = "New York",
            WeightScore = 0,
            Status = LeadStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var score = service.Calculate(lead, "marketing manager chicago", null);
        score.Should().Be(0.0, "lead has no matching keywords");
    }

    [Fact]
    public void Calculate_returns_high_score_for_exact_title_match()
    {
        var service = new LeadScoringService();
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            CampaignId = Guid.NewGuid(),
            FullName = "Jane Smith",
            ProfileUrl = "https://linkedin.com/in/janesmith",
            Title = "Engineering Manager",
            Headline = "Leading technical teams",
            Location = "San Francisco",
            WeightScore = 0,
            Status = LeadStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var score = service.Calculate(lead, "engineering manager", null);
        score.Should().BeGreaterThan(40.0, "title has exact keyword matches");
    }

    [Fact]
    public void Calculate_uses_custom_heuristics_when_provided()
    {
        var service = new LeadScoringService();
        var heuristics = new Artifact
        {
            Id = Guid.NewGuid(),
            CampaignId = Guid.NewGuid(),
            Type = ArtifactType.Heuristics,
            Key = "scoring",
            ContentJson = JsonSerializer.Serialize(
                new
                {
                    titleWeight = 0.8,
                    headlineWeight = 0.1,
                    locationWeight = 0.1,
                }),
            Source = ArtifactSource.User,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
        };
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            CampaignId = Guid.NewGuid(),
            FullName = "Bob Johnson",
            ProfileUrl = "https://linkedin.com/in/bobjohnson",
            Title = "Product Manager",
            Headline = "Building products users love",
            Location = "Seattle",
            WeightScore = 0,
            Status = LeadStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var score = service.Calculate(lead, "product manager", heuristics);
        score.Should().BeGreaterThan(60.0, "custom heuristics weight title heavily");
    }

    [Fact]
    public void Score_returns_leads_sorted_by_relevance()
    {
        var service = new LeadScoringService();
        var campaignId = Guid.NewGuid();
        var leads = new List<Lead>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CampaignId = campaignId,
                FullName = "Alice",
                ProfileUrl = "https://linkedin.com/in/alice",
                Title = "Marketing Coordinator",
                Headline = "Social media specialist",
                Location = "Boston",
                WeightScore = 0,
                Status = LeadStatus.New,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.NewGuid(),
                CampaignId = campaignId,
                FullName = "Bob",
                ProfileUrl = "https://linkedin.com/in/bob",
                Title = "Marketing Manager",
                Headline = "Growth marketing expert",
                Location = "Chicago",
                WeightScore = 0,
                Status = LeadStatus.New,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = Guid.NewGuid(),
                CampaignId = campaignId,
                FullName = "Charlie",
                ProfileUrl = "https://linkedin.com/in/charlie",
                Title = "Software Engineer",
                Headline = "Full stack developer",
                Location = "Austin",
                WeightScore = 0,
                Status = LeadStatus.New,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };
        var sorted = service.Score(leads, "marketing manager growth", null);
        sorted.Should().HaveCount(3, "all leads are scored");
        sorted[0].FullName.Should().Be("Bob", "Bob has best match");
        sorted[2].FullName.Should().Be("Charlie", "Charlie has worst match");
    }

    [Fact]
    public void Calculate_handles_empty_audience_gracefully()
    {
        var service = new LeadScoringService();
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            CampaignId = Guid.NewGuid(),
            FullName = "Test User",
            ProfileUrl = "https://linkedin.com/in/test",
            Title = "Manager",
            Headline = "Professional",
            Location = "City",
            WeightScore = 0,
            Status = LeadStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var score = service.Calculate(lead, string.Empty, null);
        score.Should().Be(0.0, "empty audience produces no keywords");
    }

    [Fact]
    public void Calculate_is_case_insensitive()
    {
        var service = new LeadScoringService();
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            CampaignId = Guid.NewGuid(),
            FullName = "Test User",
            ProfileUrl = "https://linkedin.com/in/test",
            Title = "SENIOR DEVELOPER",
            Headline = "Expert in PYTHON",
            Location = "LONDON",
            WeightScore = 0,
            Status = LeadStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var score = service.Calculate(lead, "senior developer python", null);
        score.Should().BeGreaterThan(40.0, "matching is case insensitive");
    }

    [Fact]
    public void Calculate_filters_short_keywords()
    {
        var service = new LeadScoringService();
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            CampaignId = Guid.NewGuid(),
            FullName = "Test User",
            ProfileUrl = "https://linkedin.com/in/test",
            Title = "VP of Sales",
            Headline = "B2B SaaS",
            Location = "SF Bay Area",
            WeightScore = 0,
            Status = LeadStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var score = service.Calculate(lead, "a b VP of Sales", null);
        score.Should().BeGreaterThan(0.0, "short keywords are ignored");
    }
}
