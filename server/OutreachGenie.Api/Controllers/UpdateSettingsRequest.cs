// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Request model for updating application settings.
/// </summary>
public sealed record UpdateSettingsRequest(
    string? LlmProvider,
    string? Model,
    double? Temperature,
    int? MaxTokens,
    int? RetryCount,
    int? TimeoutSeconds);
