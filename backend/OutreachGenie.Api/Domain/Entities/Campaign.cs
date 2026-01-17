// -----------------------------------------------------------------------
// <copyright file="Campaign.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OutreachGenie.Api.Domain.Entities;

/// <summary>
/// Represents a marketing campaign.
/// </summary>
public sealed class Campaign
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Campaign"/> class.
    /// </summary>
    public Campaign(
        Guid id,
        string name,
        CampaignPhase phase,
        DateTime createdAt,
        string metadata)
    {
        this.Id = id;
        this.Name = name;
        this.Phase = phase;
        this.CreatedAt = createdAt;
        this.UpdatedAt = createdAt;
        this.Metadata = metadata;
        this.Tasks = new List<CampaignTask>();
        this.Leads = new List<Lead>();
        this.Artifacts = new List<Artifact>();
    }

    /// <summary>
    /// Campaign identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Campaign name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Current phase of the campaign.
    /// </summary>
    public CampaignPhase Phase { get; private set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Metadata as JSON string.
    /// </summary>
    public string Metadata { get; private set; } = string.Empty;

    /// <summary>
    /// Campaign tasks.
    /// </summary>
    public ICollection<CampaignTask> Tasks { get; private set; }

    /// <summary>
    /// Campaign leads.
    /// </summary>
    public ICollection<Lead> Leads { get; private set; }

    /// <summary>
    /// Campaign artifacts.
    /// </summary>
    public ICollection<Artifact> Artifacts { get; private set; }

    private Campaign()
    {
        // Required for EF Core
        this.Tasks = new List<CampaignTask>();
        this.Leads = new List<Lead>();
        this.Artifacts = new List<Artifact>();
    }
}

