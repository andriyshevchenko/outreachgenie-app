// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Response model for chat messages from the agent.
/// </summary>
public sealed record ChatResponse(
    Guid MessageId,
    string Content,
    DateTime Timestamp);
