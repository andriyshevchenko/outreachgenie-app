using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;

namespace OutreachGenie.Application.Interfaces;

/// <summary>
/// Repository interface for lead persistence operations.
/// </summary>
public interface ILeadRepository
{
    /// <summary>
    /// Gets a lead by identifier.
    /// </summary>
    /// <param name="id">The lead identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The lead or null if not found.</returns>
    Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all leads for a campaign sorted by weight score descending.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of leads sorted by priority.</returns>
    Task<List<Lead>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets leads by campaign and status.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="status">The lead status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of leads matching criteria.</returns>
    Task<List<Lead>> GetByStatusAsync(Guid campaignId, LeadStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top N leads by weight score for a campaign.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="count">Number of leads to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of top-ranked leads.</returns>
    Task<List<Lead>> GetTopLeadsAsync(Guid campaignId, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new lead.
    /// </summary>
    /// <param name="lead">The lead to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created lead.</returns>
    Task<Lead> CreateAsync(Lead lead, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple leads in batch.
    /// </summary>
    /// <param name="leads">The leads to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    Task CreateBatchAsync(IEnumerable<Lead> leads, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing lead.
    /// </summary>
    /// <param name="lead">The lead to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a lead.
    /// </summary>
    /// <param name="id">The lead identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
