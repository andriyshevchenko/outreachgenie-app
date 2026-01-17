// -----------------------------------------------------------------------
// <copyright file="OutreachGenieDbContext.cs" company="OutreachGenie">
// Copyright (c) OutreachGenie. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using OutreachGenie.Api.Domain.Entities;

namespace OutreachGenie.Api.Data;

/// <summary>
/// Database context for OutreachGenie application.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated public classes", Justification = "Instantiated via dependency injection")]
public sealed class OutreachGenieDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutreachGenieDbContext"/> class.
    /// </summary>
    public OutreachGenieDbContext(DbContextOptions<OutreachGenieDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Campaigns dataset.
    /// </summary>
    public DbSet<Campaign> Campaigns => this.Set<Campaign>();

    /// <summary>
    /// Campaign tasks dataset.
    /// </summary>
    public DbSet<CampaignTask> CampaignTasks => this.Set<CampaignTask>();

    /// <summary>
    /// Leads dataset.
    /// </summary>
    public DbSet<Lead> Leads => this.Set<Lead>();

    /// <summary>
    /// Events dataset.
    /// </summary>
    public DbSet<DomainEvent> Events => this.Set<DomainEvent>();

    /// <summary>
    /// Artifacts dataset.
    /// </summary>
    public DbSet<Artifact> Artifacts => this.Set<Artifact>();

    /// <summary>
    /// Agent threads dataset.
    /// </summary>
    public DbSet<AgentThread> AgentThreads => this.Set<AgentThread>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        // Campaign configuration
        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.ToTable("Campaigns");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phase).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Metadata).IsRequired();

            entity.HasMany(e => e.Tasks)
                .WithOne(t => t.Campaign)
                .HasForeignKey(t => t.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Leads)
                .WithOne(l => l.Campaign)
                .HasForeignKey(l => l.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Artifacts)
                .WithOne(a => a.Campaign)
                .HasForeignKey(a => a.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CampaignTask configuration
        modelBuilder.Entity<CampaignTask>(entity =>
        {
            entity.ToTable("CampaignTasks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(e => new { e.CampaignId, e.OrderIndex });
        });

        // Lead configuration
        modelBuilder.Entity<Lead>(entity =>
        {
            entity.ToTable("Leads");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Score).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Data).IsRequired();
            entity.HasIndex(e => e.CampaignId);
        });

        // Event configuration
        modelBuilder.Entity<DomainEvent>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Actor).IsRequired().HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Payload).IsRequired();
            entity.HasIndex(e => e.CampaignId);
            entity.HasIndex(e => e.Timestamp);
        });

        // Artifact configuration
        modelBuilder.Entity<Artifact>(entity =>
        {
            entity.ToTable("Artifacts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.MimeType).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.CampaignId);
        });

        // AgentThread configuration
        modelBuilder.Entity<AgentThread>(entity =>
        {
            entity.ToTable("AgentThreads");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ThreadId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.State).IsRequired();
            entity.HasIndex(e => e.ThreadId).IsUnique();
            entity.HasIndex(e => e.CampaignId);

            entity.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(e => e.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

