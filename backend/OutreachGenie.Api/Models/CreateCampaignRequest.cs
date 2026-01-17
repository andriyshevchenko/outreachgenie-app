// -----------------------------------------------------------------------
// <copyright file="CreateCampaignRequest.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OutreachGenie.Api.Models;

/// <summary>
/// Request to create a campaign.
/// </summary>
public sealed class CreateCampaignRequest
{
    /// <summary>
    /// Campaign name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

