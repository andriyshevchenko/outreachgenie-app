// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

namespace OutreachGenie.Application.Services.LeadScoring;

/// <summary>
/// Default balanced heuristics configuration for lead scoring.
/// Provides standard weighting: 50% title, 30% headline, 20% location.
/// Usage: var heuristics = new DefaultScoringHeuristics().Value();
/// </summary>
public sealed class DefaultScoringHeuristics
{
    private readonly ScoringHeuristics heuristics;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultScoringHeuristics"/> class.
    /// </summary>
    public DefaultScoringHeuristics()
    {
        this.heuristics = new ScoringHeuristics(0.5, 0.3, 0.2);
    }

    /// <summary>
    /// Returns the default scoring heuristics configuration.
    /// </summary>
    /// <returns>Default heuristics with balanced weights.</returns>
    public ScoringHeuristics Value()
    {
        return this.heuristics;
    }
}
