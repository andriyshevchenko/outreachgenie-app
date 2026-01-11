// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Request model for sending chat messages.
/// </summary>
public sealed record SendMessageRequest(
    [property: JsonRequired] Guid CampaignId,
    string Message);
