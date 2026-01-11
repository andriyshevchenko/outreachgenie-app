// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

namespace OutreachGenie.Application.Services.LeadScoring;

/// <summary>
/// Configuration for lead scoring heuristics.
/// Defines weights for different matching criteria when calculating lead relevance scores.
/// Stored as a heuristics artifact and user-editable via JSON.
/// </summary>
/// <param name="title">Weight for job title relevance (0.0 to 1.0).</param>
/// <param name="headline">Weight for headline keyword matching (0.0 to 1.0).</param>
/// <param name="location">Weight for location alignment (0.0 to 1.0).</param>
public sealed class ScoringHeuristics(double title, double headline, double location)
{
    /// <summary>
    /// Gets the weight for job title matching.
    /// </summary>
    public double TitleWeight { get; } = title;

    /// <summary>
    /// Gets the weight for headline keyword matching.
    /// </summary>
    public double HeadlineWeight { get; } = headline;

    /// <summary>
    /// Gets the weight for location alignment.
    /// </summary>
    public double LocationWeight { get; } = location;
}
