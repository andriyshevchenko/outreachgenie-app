using OutreachGenie.Domain.Entities;

namespace OutreachGenie.Application.Services;

/// <summary>
/// Represents complete campaign state at a point in time.
/// </summary>
public sealed class CampaignState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CampaignState"/> class.
    /// </summary>
    /// <param name="campaign">Campaign entity.</param>
    /// <param name="tasks">Campaign tasks.</param>
    /// <param name="artifacts">Campaign artifacts.</param>
    /// <param name="leads">Campaign leads.</param>
    public CampaignState(
        Campaign campaign,
        IReadOnlyList<CampaignTask> tasks,
        IReadOnlyList<Artifact> artifacts,
        IReadOnlyList<Lead> leads)
    {
        this.Campaign = campaign;
        this.Tasks = tasks;
        this.Artifacts = artifacts;
        this.Leads = leads;
    }

    /// <summary>
    /// Gets the campaign entity.
    /// </summary>
    public Campaign Campaign { get; }

    /// <summary>
    /// Gets campaign tasks.
    /// </summary>
    public IReadOnlyList<CampaignTask> Tasks { get; }

    /// <summary>
    /// Gets campaign artifacts.
    /// </summary>
    public IReadOnlyList<Artifact> Artifacts { get; }

    /// <summary>
    /// Gets campaign leads.
    /// </summary>
    public IReadOnlyList<Lead> Leads { get; }
}
