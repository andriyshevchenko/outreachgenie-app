// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json;
using OutreachGenie.Domain.Entities;

namespace OutreachGenie.Application.Services.LeadScoring;

/// <summary>
/// Calculates lead relevance scores using weighted heuristics.
/// Scores are based on keyword matching against target audience criteria.
/// No profile visits are performed per specification requirement.
/// </summary>
public sealed class LeadScoringService : ILeadScoringService
{
    /// <summary>
    /// Calculates a weighted relevance score for a lead.
    /// </summary>
    /// <param name="lead">The lead to score.</param>
    /// <param name="audience">Target audience criteria from campaign.</param>
    /// <param name="heuristics">Scoring configuration artifact.</param>
    /// <returns>Score between 0.0 and 100.0.</returns>
    public double Calculate(Lead lead, string audience, Artifact? heuristics)
    {
        var config = Parse(heuristics);
        var keywords = Extract(audience);
        var titleScore = Match(lead.Title, keywords) * config.TitleWeight;
        var headlineScore = Match(lead.Headline, keywords) * config.HeadlineWeight;
        var locationScore = Match(lead.Location, keywords) * config.LocationWeight;
        return (titleScore + headlineScore + locationScore) * 100.0;
    }

    /// <summary>
    /// Scores multiple leads and returns them sorted by relevance.
    /// </summary>
    /// <param name="leads">Collection of leads to score.</param>
    /// <param name="audience">Target audience criteria from campaign.</param>
    /// <param name="heuristics">Scoring configuration artifact.</param>
    /// <returns>Leads sorted by score descending.</returns>
    public IReadOnlyList<Lead> Score(
        IEnumerable<Lead> leads,
        string audience,
        Artifact? heuristics)
    {
        var scored = leads.Select(
            lead =>
            {
                var score = Calculate(lead, audience, heuristics);
                return (lead, score);
            });
        return scored
            .OrderByDescending(x => x.score)
            .Select(x => x.lead)
            .ToList();
    }

    private static ScoringHeuristics Parse(Artifact? artifact)
    {
        if (artifact == null)
        {
            return ScoringHeuristics.Default();
        }

        var json = JsonDocument.Parse(artifact.ContentJson);
        var root = json.RootElement;
        var title = root.GetProperty("titleWeight").GetDouble();
        var headline = root.GetProperty("headlineWeight").GetDouble();
        var location = root.GetProperty("locationWeight").GetDouble();
        return new ScoringHeuristics(title, headline, location);
    }

    private static string[] Extract(string audience)
    {
        return audience
            .Split([' ', ',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.ToLowerInvariant().Trim())
            .Where(k => k.Length > 2)
            .Distinct()
            .ToArray();
    }

    private static double Match(string text, string[] keywords)
    {
        if (keywords.Length == 0)
        {
            return 0.0;
        }

        var lower = text.ToLowerInvariant();
        var matches = keywords.Count(k => lower.Contains(k));
        return (double)matches / keywords.Length;
    }
}
