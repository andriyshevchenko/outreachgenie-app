using OutreachGenie.Api.Domain.Abstractions;
using OutreachGenie.Api.Domain.Entities;

namespace OutreachGenie.Api.Infrastructure.Repositories;

/// <summary>
/// Repository for campaign operations.
/// </summary>
public interface ICampaignRepository : IRepository<Campaign>
{
    /// <summary>
    /// Loads a campaign with its tasks.
    /// </summary>
    Task<Campaign?> LoadWithTasks(Guid campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a campaign with all related entities.
    /// </summary>
    Task<Campaign?> LoadComplete(Guid campaignId, CancellationToken cancellationToken = default);
}
