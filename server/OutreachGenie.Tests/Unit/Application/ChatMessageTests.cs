// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using FluentAssertions;
using OutreachGenie.Application.Services.Llm;
using Xunit;

namespace OutreachGenie.Tests.Unit.Application;

/// <summary>
/// Unit tests for ChatMessage model.
/// </summary>
public sealed class ChatMessageTests
{
    /// <summary>
    /// Tests that ChatMessage initializes with correct values.
    /// </summary>
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var message = new ChatMessage("user", "Test message content");

        message.Role.Should().Be("user");
        message.Content.Should().Be("Test message content");
    }

    /// <summary>
    /// Tests that ChatMessage supports different roles.
    /// </summary>
    [Fact]
    public void Role_ShouldSupportUserAndAssistant()
    {
        var userMessage = new ChatMessage("user", "Hello");
        var assistantMessage = new ChatMessage("assistant", "Hi there");

        userMessage.Role.Should().Be("user");
        assistantMessage.Role.Should().Be("assistant");
    }

    /// <summary>
    /// Tests that ChatMessage can contain empty content.
    /// </summary>
    [Fact]
    public void Content_CanBeEmpty()
    {
        var message = new ChatMessage("system", string.Empty);

        message.Content.Should().BeEmpty();
    }
}
