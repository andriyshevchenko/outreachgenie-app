using Microsoft.AspNetCore.Mvc;
using OutreachGenie.Api.Domain.Entities;
using OutreachGenie.Api.Infrastructure.Repositories;
using OutreachGenie.Api.Models;

namespace OutreachGenie.Api.Controllers;

/// <summary>
/// API controller for campaign management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class CampaignsController : ControllerBase
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly ILogger<CampaignsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CampaignsController"/> class.
    /// </summary>
    public CampaignsController(
        ICampaignRepository campaignRepository,
        ILogger<CampaignsController> logger)
    {
        this._campaignRepository = campaignRepository;
        this._logger = logger;
    }

    /// <summary>
    /// Creates a new campaign.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CampaignDto>> CreateCampaign(
        [FromBody] CreateCampaignRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        this._logger.LogInformation("Creating new campaign: {Name}", request.Name);

        Campaign campaign = new(
            Guid.NewGuid(),
            request.Name,
            CampaignPhase.Planning,
            DateTime.UtcNow,
            "{}");

        await this._campaignRepository.Add(campaign, cancellationToken);
        await this._campaignRepository.SaveChanges(cancellationToken);

        return this.CreatedAtAction(
            nameof(this.GetCampaign),
            new { id = campaign.Id },
            CampaignDto.FromEntity(campaign));
    }

    /// <summary>
    /// Gets a campaign by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CampaignDto>> GetCampaign(
        Guid id,
        CancellationToken cancellationToken)
    {
        Campaign? campaign = await this._campaignRepository.LoadComplete(id, cancellationToken);

        if (campaign == null)
        {
            return this.NotFound();
        }

        return CampaignDto.FromEntity(campaign);
    }

    /// <summary>
    /// Gets campaign status.
    /// </summary>
    [HttpGet("{id}/status")]
    public async Task<ActionResult<CampaignStatusDto>> GetCampaignStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        Campaign? campaign = await this._campaignRepository.LoadComplete(id, cancellationToken);

        if (campaign == null)
        {
            return this.NotFound();
        }

        return new CampaignStatusDto
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Phase = campaign.Phase.ToString(),
            TotalTasks = campaign.Tasks.Count,
            CompletedTasks = campaign.Tasks.Count(t => t.Status == Domain.Entities.TaskStatus.Completed),
            PendingTasks = campaign.Tasks.Count(t => t.Status == Domain.Entities.TaskStatus.Pending),
            TotalLeads = campaign.Leads.Count,
            ScoredLeads = campaign.Leads.Count(l => l.Score.HasValue),
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt,
        };
    }

    /// <summary>
    /// Gets all campaigns.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CampaignDto>>> GetAllCampaigns(CancellationToken cancellationToken)
    {
        IEnumerable<Campaign> campaigns = await this._campaignRepository.GetAll(cancellationToken);
        return this.Ok(campaigns.Select(CampaignDto.FromEntity));
    }
}
