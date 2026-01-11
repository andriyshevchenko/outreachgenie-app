// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;
using OutreachGenie.Domain.Enums;

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Request model for creating artifacts.
/// </summary>
public sealed record CreateArtifactRequest(
    [property: JsonRequired] Guid CampaignId,
    [property: JsonRequired] ArtifactType Type,
    string Key,
    string ContentJson,
    [property: JsonRequired] ArtifactSource Source,
    [property: JsonRequired] int Version);
