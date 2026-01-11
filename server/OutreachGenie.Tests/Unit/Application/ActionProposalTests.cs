// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using FluentAssertions;
using OutreachGenie.Application.Services;
using Xunit;

namespace OutreachGenie.Tests.Unit.Application;

/// <summary>
/// Unit tests for ActionProposal model.
/// </summary>
public sealed class ActionProposalTests
{
    /// <summary>
    /// Tests that ActionProposal initializes with all properties.
    /// </summary>
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var taskId = Guid.NewGuid();
        var parametersJson = "{\"query\":\"CTO\",\"location\":\"San Francisco\"}";

        var proposal = new ActionProposal
        {
            ActionType = "search_linkedin",
            TaskId = taskId,
            Parameters = parametersJson,
        };

        proposal.ActionType.Should().Be("search_linkedin");
        proposal.TaskId.Should().Be(taskId);
        proposal.Parameters.Should().Contain("CTO");
    }

    /// <summary>
    /// Tests that ActionProposal supports empty parameters.
    /// </summary>
    [Fact]
    public void Parameters_CanBeEmpty()
    {
        var proposal = new ActionProposal
        {
            ActionType = "get_status",
            TaskId = Guid.NewGuid(),
            Parameters = "{}",
        };

        proposal.Parameters.Should().Be("{}");
    }

    /// <summary>
    /// Tests that ActionProposal can represent different action types.
    /// </summary>
    [Fact]
    public void ActionType_ShouldSupportVariousActions()
    {
        var searchProposal = new ActionProposal { ActionType = "search", TaskId = Guid.NewGuid(), Parameters = "{}" };
        var writeProposal = new ActionProposal { ActionType = "write_file", TaskId = Guid.NewGuid(), Parameters = "{}" };
        var scoreProposal = new ActionProposal { ActionType = "score_leads", TaskId = Guid.NewGuid(), Parameters = "{}" };

        searchProposal.ActionType.Should().Be("search");
        writeProposal.ActionType.Should().Be("write_file");
        scoreProposal.ActionType.Should().Be("score_leads");
    }

    /// <summary>
    /// Tests that ActionProposal parameters support complex JSON.
    /// </summary>
    [Fact]
    public void Parameters_ShouldSupportComplexJson()
    {
        var parametersJson = "{\"filters\":{\"minScore\":0.7,\"status\":\"active\"},\"limit\":50}";

        var proposal = new ActionProposal
        {
            ActionType = "filter_leads",
            TaskId = Guid.NewGuid(),
            Parameters = parametersJson,
        };

        proposal.Parameters.Should().Contain("minScore");
        proposal.Parameters.Should().Contain("active");
    }
}
