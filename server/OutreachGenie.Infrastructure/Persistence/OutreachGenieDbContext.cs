using Microsoft.EntityFrameworkCore;
using OutreachGenie.Domain.Entities;

namespace OutreachGenie.Infrastructure.Persistence;

/// <summary>
/// Database context for OutreachGenie application managing campaigns, tasks, artifacts, and leads.
/// </summary>
public sealed class OutreachGenieDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutreachGenieDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public OutreachGenieDbContext(DbContextOptions<OutreachGenieDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the campaigns collection.
    /// </summary>
    public DbSet<Campaign> Campaigns { get; set; } = null!;

    /// <summary>
    /// Gets or sets the tasks collection.
    /// </summary>
    public DbSet<CampaignTask> Tasks { get; set; } = null!;

    /// <summary>
    /// Gets or sets the artifacts collection.
    /// </summary>
    public DbSet<Artifact> Artifacts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the leads collection.
    /// </summary>
    public DbSet<Lead> Leads { get; set; } = null!;

    /// <summary>
    /// Configures entity mappings and relationships.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TargetAudience).HasMaxLength(500);
            entity.Property(e => e.WorkingDirectory).HasMaxLength(500);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<CampaignTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => new { e.CampaignId, e.Status });
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Campaign)
                .WithMany(c => c.Tasks)
                .HasForeignKey(e => e.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Artifact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ContentJson).IsRequired();
            entity.Property(e => e.Key).HasMaxLength(200);
            entity.HasIndex(e => new { e.CampaignId, e.Type, e.Key });
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Campaign)
                .WithMany(c => c.Artifacts)
                .HasForeignKey(e => e.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ProfileUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Headline).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.HasIndex(e => new { e.CampaignId, e.Status });
            entity.HasIndex(e => e.WeightScore);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Campaign)
                .WithMany(c => c.Leads)
                .HasForeignKey(e => e.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
