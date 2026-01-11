// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using OutreachGenie.Domain.Entities;

namespace OutreachGenie.Application.Services.LeadScoring;

/// <summary>
/// Calculates relevance scores for prospect leads based on configurable heuristics.
/// Scores are determined by matching job titles, headlines, and locations against campaign criteria.
/// Does not visit profiles per specification requirement eight.
/// </summary>
/// <example>
/// var service = new LeadScoringService();
/// var heuristics = await artifactRepo.GetLatestByTypeAsync(campaignId, ArtifactType.Heuristics);
/// var score = service.Calculate(lead, campaign.TargetAudience, heuristics);
/// </example>
public interface ILeadScoringService
{
    /// <summary>
    /// Calculates a weighted relevance score for a lead.
    /// </summary>
    /// <param name="lead">The lead to score.</param>
    /// <param name="audience">Target audience criteria from campaign.</param>
    /// <param name="heuristics">Scoring configuration artifact.</param>
    /// <returns>Score between 0.0 and 100.0.</returns>
    double Calculate(Lead lead, string audience, Artifact? heuristics);

    /// <summary>
    /// Scores multiple leads and returns them sorted by relevance.
    /// </summary>
    /// <param name="leads">Collection of leads to score.</param>
    /// <param name="audience">Target audience criteria from campaign.</param>
    /// <param name="heuristics">Scoring configuration artifact.</param>
    /// <returns>Leads sorted by score descending.</returns>
    IReadOnlyList<Lead> Score(
        IEnumerable<Lead> leads,
        string audience,
        Artifact? heuristics);
}
