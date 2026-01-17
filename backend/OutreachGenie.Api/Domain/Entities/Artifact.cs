using System.Diagnostics.CodeAnalysis;

// -----------------------------------------------------------------------
// <copyright file="Artifact.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace OutreachGenie.Api.Domain.Entities;

/// <summary>
/// Represents a file artifact generated during a campaign.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated public classes", Justification = "Instantiated by Entity Framework Core")]
public sealed class Artifact
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Artifact"/> class.
    /// </summary>
    public Artifact(
        Guid id,
        Guid campaignId,
        string fileName,
        string filePath,
        string mimeType,
        int version,
        DateTime createdAt)
    {
        this.Id = id;
        this.CampaignId = campaignId;
        this.FileName = fileName;
        this.FilePath = filePath;
        this.MimeType = mimeType;
        this.Version = version;
        this.CreatedAt = createdAt;
    }

    /// <summary>
    /// Artifact identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Campaign identifier.
    /// </summary>
    public Guid CampaignId { get; private set; }

    /// <summary>
    /// File name.
    /// </summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// File path on disk.
    /// </summary>
    public string FilePath { get; private set; } = string.Empty;

    /// <summary>
    /// MIME type.
    /// </summary>
    public string MimeType { get; private set; } = string.Empty;

    /// <summary>
    /// Version number.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Deletion timestamp.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Parent campaign.
    /// </summary>
    public Campaign Campaign { get; private set; } = null!;

    private Artifact()
    {
        // Required for EF Core
    }
}

