using OutreachGenie.Domain.Entities;

namespace OutreachGenie.Application.Interfaces;

/// <summary>
/// Repository interface for campaign persistence operations.
/// </summary>
public interface ICampaignRepository
{
    /// <summary>
    /// Gets a campaign by identifier including related tasks and artifacts.
    /// </summary>
    /// <param name="id">The campaign identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The campaign or null if not found.</returns>
    Task<Campaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a campaign with all related entities for full state recovery.
    /// </summary>
    /// <param name="id">The campaign identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The campaign with all related data or null if not found.</returns>
    Task<Campaign?> GetWithAllRelatedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all campaigns with optional filtering.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of campaigns.</returns>
    Task<List<Campaign>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new campaign.
    /// </summary>
    /// <param name="campaign">The campaign to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created campaign.</returns>
    Task<Campaign> CreateAsync(Campaign campaign, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing campaign.
    /// </summary>
    /// <param name="campaign">The campaign to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    Task UpdateAsync(Campaign campaign, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a campaign and all related entities.
    /// </summary>
    /// <param name="id">The campaign identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
