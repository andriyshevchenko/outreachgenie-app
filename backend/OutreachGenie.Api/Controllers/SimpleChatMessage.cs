// -----------------------------------------------------------------------
// <copyright file="SimpleChatMessage.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Simple chat message format for JSON deserialization.
/// </summary>
public sealed class SimpleChatMessage
{
    /// <summary>
    /// Gets or sets the role (e.g., "user", "assistant").
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    public required string Content { get; set; }
}

