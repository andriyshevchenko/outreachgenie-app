// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using FluentAssertions;
using OutreachGenie.Application.Services.Llm;
using Xunit;

namespace OutreachGenie.Tests.Unit.Application;

/// <summary>
/// Unit tests for LlmConfiguration model.
/// </summary>
public sealed class LlmConfigurationTests
{
    /// <summary>
    /// Tests that LlmConfiguration initializes with correct values.
    /// </summary>
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var config = new LlmConfiguration(
            0.7,
            2000,
            "gpt-4",
            3,
            30);

        config.Model.Should().Be("gpt-4");
        config.Temperature.Should().Be(0.7);
        config.MaxTokens.Should().Be(2000);
        config.MaxRetries.Should().Be(3);
        config.TimeoutSeconds.Should().Be(30);
    }

    /// <summary>
    /// Tests that LlmConfiguration supports valid temperature ranges.
    /// </summary>
    [Fact]
    public void Temperature_ShouldSupportValidRanges()
    {
        var lowTemp = new LlmConfiguration(0.0, 1000, "gpt-3.5-turbo", 1, 10);
        var highTemp = new LlmConfiguration(2.0, 1000, "gpt-4", 1, 10);

        lowTemp.Temperature.Should().Be(0.0);
        highTemp.Temperature.Should().Be(2.0);
    }

    /// <summary>
    /// Tests that LlmConfiguration can be created with different models.
    /// </summary>
    [Fact]
    public void Model_ShouldAcceptDifferentProviders()
    {
        var openAi = new LlmConfiguration(0.5, 4000, "gpt-4-turbo", 2, 60);
        var claude = new LlmConfiguration(0.8, 3000, "claude-3-opus", 3, 45);

        openAi.Model.Should().Be("gpt-4-turbo");
        claude.Model.Should().Be("claude-3-opus");
    }
}
