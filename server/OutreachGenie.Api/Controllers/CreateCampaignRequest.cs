// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Request for creating a new campaign.
/// </summary>
/// <param name="Name">Campaign name.</param>
/// <param name="TargetAudience">Description of target audience.</param>
public sealed record CreateCampaignRequest(string Name, string TargetAudience);
