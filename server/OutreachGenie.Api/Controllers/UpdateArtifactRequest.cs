// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// Request model for updating artifacts.
/// </summary>
public sealed record UpdateArtifactRequest(
    string ContentJson,
    [property: JsonRequired] int Version);
