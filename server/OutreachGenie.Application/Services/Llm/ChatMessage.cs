// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

namespace OutreachGenie.Application.Services.Llm;

/// <summary>
/// Represents a message in a chat conversation.
/// Used for LLM conversation history and narration context.
/// </summary>
/// <param name="role">The role of the message sender (system, user, assistant).</param>
/// <param name="content">The text content of the message.</param>
public sealed class ChatMessage(string role, string content)
{
    /// <summary>
    /// Gets the role of the message sender.
    /// </summary>
    public string Role { get; } = role;

    /// <summary>
    /// Gets the text content of the message.
    /// </summary>
    public string Content { get; } = content;
}
