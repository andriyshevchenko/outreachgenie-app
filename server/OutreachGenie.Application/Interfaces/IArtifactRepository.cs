using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;

namespace OutreachGenie.Application.Interfaces;

/// <summary>
/// Repository interface for artifact persistence operations.
/// </summary>
public interface IArtifactRepository
{
    /// <summary>
    /// Gets an artifact by identifier.
    /// </summary>
    /// <param name="id">The artifact identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The artifact or null if not found.</returns>
    Task<Artifact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an artifact by campaign, type, and key.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="type">The artifact type.</param>
    /// <param name="key">The artifact key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The artifact or null if not found.</returns>
    Task<Artifact?> GetByKeyAsync(Guid campaignId, ArtifactType type, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all artifacts of a specific type for a campaign.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="type">The artifact type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of artifacts.</returns>
    Task<List<Artifact>> GetByTypeAsync(Guid campaignId, ArtifactType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all artifacts for a campaign.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of artifacts.</returns>
    Task<List<Artifact>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new artifact.
    /// </summary>
    /// <param name="artifact">The artifact to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created artifact.</returns>
    Task<Artifact> CreateAsync(Artifact artifact, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing artifact (creates new version).
    /// </summary>
    /// <param name="artifact">The artifact to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    Task UpdateAsync(Artifact artifact, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an artifact.
    /// </summary>
    /// <param name="id">The artifact identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
