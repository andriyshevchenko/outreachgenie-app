// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Model for application settings and configuration.
/// </summary>
public sealed record Settings(
    string LlmProvider,
    string Model,
    double Temperature,
    int MaxTokens,
    int RetryCount,
    int TimeoutSeconds);
