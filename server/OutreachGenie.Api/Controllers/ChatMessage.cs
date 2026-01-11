// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Model for chat message history entries.
/// </summary>
public sealed record ChatMessage(
    Guid Id,
    string Role,
    string Content,
    DateTime Timestamp);
