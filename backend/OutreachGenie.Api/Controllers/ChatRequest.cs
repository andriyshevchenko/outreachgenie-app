// -----------------------------------------------------------------------
// <copyright file="ChatRequest.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Request model for agent chat interactions.
/// Simple POCO that accepts {role, content} JSON format.
/// </summary>
public sealed class ChatRequest
{
    /// <summary>
    /// Gets or initializes the list of simple chat messages.
    /// </summary>
    public required List<SimpleChatMessage> Messages { get; init; }
}

