using FluentAssertions;
using Moq;
using OutreachGenie.Application.Interfaces;
using OutreachGenie.Application.Services;
using OutreachGenie.Application.Services.Llm;
using OutreachGenie.Application.Services.Mcp;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;
using Xunit;
using TaskStatus = OutreachGenie.Domain.Enums.TaskStatus;

namespace OutreachGenie.Tests.Unit.Services;

/// <summary>
/// Unit tests for DeterministicController.
/// </summary>
public sealed class DeterministicControllerTests
{
    private readonly Mock<ICampaignRepository> campaignRepository;
    private readonly Mock<ITaskRepository> taskRepository;
    private readonly Mock<IArtifactRepository> artifactRepository;
    private readonly Mock<ILeadRepository> leadRepository;
    private readonly Mock<ILlmProvider> llmProvider;
    private readonly Mock<IMcpToolRegistry> mcpRegistry;
    private readonly IDeterministicController controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicControllerTests"/> class.
    /// </summary>
    public DeterministicControllerTests()
    {
        this.campaignRepository = new Mock<ICampaignRepository>();
        this.taskRepository = new Mock<ITaskRepository>();
        this.artifactRepository = new Mock<IArtifactRepository>();
        this.leadRepository = new Mock<ILeadRepository>();
        this.llmProvider = new Mock<ILlmProvider>();
        this.mcpRegistry = new Mock<IMcpToolRegistry>();
        this.controller = new DeterministicController(
            this.campaignRepository.Object,
            this.taskRepository.Object,
            this.artifactRepository.Object,
            this.leadRepository.Object,
            this.llmProvider.Object,
            this.mcpRegistry.Object);
    }

    /// <summary>
    /// Tests that ReloadStateAsync loads complete campaign state.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReloadStateAsync_ShouldLoadCompleteCampaignState()
    {
        var campaignId = Guid.NewGuid();
        var campaign = new Campaign
        {
            Id = campaignId,
            Name = "Test Campaign",
            Status = CampaignStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var task = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Status = TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        var artifact = new Artifact
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Type = ArtifactType.Context,
            CreatedAt = DateTime.UtcNow,
        };
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            FullName = "John Doe",
            CreatedAt = DateTime.UtcNow,
        };
        this.campaignRepository.Setup(r => r.GetWithAllRelatedAsync(campaignId, default)).ReturnsAsync(campaign);
        this.taskRepository.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<CampaignTask> { task });
        this.artifactRepository.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<Artifact> { artifact });
        this.leadRepository.Setup(r => r.GetByCampaignIdAsync(campaignId, default)).ReturnsAsync(new List<Lead> { lead });

        var state = await this.controller.ReloadStateAsync(campaignId);

        state.Campaign.Should().Be(campaign);
        state.Tasks.Should().Contain(task);
        state.Artifacts.Should().Contain(artifact);
        state.Leads.Should().Contain(lead);
    }

    /// <summary>
    /// Tests that SelectNextTask returns earliest pending task.
    /// </summary>
    [Fact]
    public void SelectNextTask_ShouldReturnEarliestPendingTask()
    {
        var campaign = new Campaign { Id = Guid.NewGuid(), Status = CampaignStatus.Active };
        var task1 = new CampaignTask { Id = Guid.NewGuid(), Status = TaskStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        var task2 = new CampaignTask { Id = Guid.NewGuid(), Status = TaskStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
        var task3 = new CampaignTask { Id = Guid.NewGuid(), Status = TaskStatus.Done, CreatedAt = DateTime.UtcNow.AddMinutes(-15) };
        var state = new CampaignState(campaign, new List<CampaignTask> { task1, task2, task3 }, [], []);

        var next = DeterministicController.SelectNextTask(state);

        next.Should().Be(task1);
    }

    /// <summary>
    /// Tests that SelectNextTask returns null when campaign is paused.
    /// </summary>
    [Fact]
    public void SelectNextTask_ShouldReturnNullWhenCampaignPaused()
    {
        var campaign = new Campaign { Id = Guid.NewGuid(), Status = CampaignStatus.Paused };
        var task = new CampaignTask { Id = Guid.NewGuid(), Status = TaskStatus.Pending, CreatedAt = DateTime.UtcNow };
        var state = new CampaignState(campaign, new List<CampaignTask> { task }, [], []);

        var next = DeterministicController.SelectNextTask(state);

        next.Should().BeNull();
    }

    /// <summary>
    /// Tests that ValidateProposal rejects missing task.
    /// </summary>
    [Fact]
    public void ValidateProposal_ShouldRejectMissingTask()
    {
        var campaign = new Campaign { Id = Guid.NewGuid(), Status = CampaignStatus.Active };
        var state = new CampaignState(campaign, [], [], []);
        var proposal = new ActionProposal { TaskId = Guid.NewGuid(), ActionType = "execute" };

        var result = DeterministicController.ValidateProposal(proposal, state);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    /// <summary>
    /// Tests that ValidateProposal rejects non-pending task.
    /// </summary>
    [Fact]
    public void ValidateProposal_ShouldRejectNonPendingTask()
    {
        var campaign = new Campaign { Id = Guid.NewGuid(), Status = CampaignStatus.Active };
        var task = new CampaignTask { Id = Guid.NewGuid(), Status = TaskStatus.Done };
        var state = new CampaignState(campaign, new List<CampaignTask> { task }, [], []);
        var proposal = new ActionProposal { TaskId = task.Id, ActionType = "execute" };

        var result = DeterministicController.ValidateProposal(proposal, state);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cannot execute");
    }

    /// <summary>
    /// Tests that ValidateProposal accepts valid pending task.
    /// </summary>
    [Fact]
    public void ValidateProposal_ShouldAcceptValidPendingTask()
    {
        var campaign = new Campaign { Id = Guid.NewGuid(), Status = CampaignStatus.Active };
        var task = new CampaignTask { Id = Guid.NewGuid(), Status = TaskStatus.Pending };
        var state = new CampaignState(campaign, new List<CampaignTask> { task }, [], []);
        var proposal = new ActionProposal { TaskId = task.Id, ActionType = "execute" };

        var result = DeterministicController.ValidateProposal(proposal, state);

        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Tests that UpdateTaskAfterExecutionAsync sets completion timestamp for done status.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task UpdateTaskAfterExecutionAsync_ShouldSetCompletionTimestampWhenDone()
    {
        var task = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = Guid.NewGuid(),
            Status = TaskStatus.InProgress,
        };
        this.taskRepository.Setup(r => r.UpdateAsync(task, default)).Returns(Task.CompletedTask);

        await this.controller.UpdateTaskAfterExecutionAsync(task, TaskStatus.Done, "{\"result\":\"success\"}");

        task.Status.Should().Be(TaskStatus.Done);
        task.CompletedAt.Should().NotBeNull();
        task.OutputJson.Should().Contain("success");
    }

    /// <summary>
    /// Tests that UpdateTaskAfterExecutionAsync handles retry logic for failed tasks.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task UpdateTaskAfterExecutionAsync_ShouldRetryFailedTaskWhenRetriesRemain()
    {
        var task = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = Guid.NewGuid(),
            Status = TaskStatus.InProgress,
            RetryCount = 0,
            MaxRetries = 3,
        };
        this.taskRepository.Setup(r => r.UpdateAsync(task, default)).Returns(Task.CompletedTask);

        await this.controller.UpdateTaskAfterExecutionAsync(task, TaskStatus.Failed, null, "Network timeout");

        task.Status.Should().Be(TaskStatus.Retrying);
        task.RetryCount.Should().Be(1);
        task.ErrorMessage.Should().Contain("timeout");
    }

    /// <summary>
    /// Tests that CreateAuditLogAsync creates artifact with action details.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CreateAuditLogAsync_ShouldCreateArtifactWithActionDetails()
    {
        var campaignId = Guid.NewGuid();
        this.artifactRepository.Setup(r => r.CreateAsync(It.IsAny<Artifact>(), default)).ReturnsAsync((Artifact a, CancellationToken _) => a);

        var log = await this.controller.CreateAuditLogAsync(campaignId, "task_executed", "{\"taskId\":\"abc\"}");

        log.CampaignId.Should().Be(campaignId);
        log.Type.Should().Be(ArtifactType.Arbitrary);
        log.Source.Should().Be(ArtifactSource.Agent);
        log.ContentJson.Should().Contain("task_executed");
    }

    /// <summary>
    /// Tests that TransitionCampaignStatusAsync validates state transitions.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task TransitionCampaignStatusAsync_ShouldValidateStateTransitions()
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Status = CampaignStatus.Initializing,
        };
        this.campaignRepository.Setup(r => r.GetByIdAsync(campaign.Id, default)).ReturnsAsync(campaign);
        this.campaignRepository.Setup(r => r.UpdateAsync(campaign, default)).Returns(Task.CompletedTask);

        await this.controller.TransitionCampaignStatusAsync(campaign.Id, CampaignStatus.Active);

        campaign.Status.Should().Be(CampaignStatus.Active);
    }

    /// <summary>
    /// Tests that TransitionCampaignStatusAsync rejects invalid transitions.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task TransitionCampaignStatusAsync_ShouldRejectInvalidTransition()
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Status = CampaignStatus.Completed,
        };
        this.campaignRepository.Setup(r => r.GetByIdAsync(campaign.Id, default)).ReturnsAsync(campaign);

        var act = async () => await this.controller.TransitionCampaignStatusAsync(campaign.Id, CampaignStatus.Active);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Invalid status transition*");
    }

    /// <summary>
    /// Tests that TransitionCampaignStatusAsync sets completion timestamp for terminal states.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task TransitionCampaignStatusAsync_ShouldSetCompletionTimestampForTerminalStates()
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Status = CampaignStatus.Active,
        };
        this.campaignRepository.Setup(r => r.GetByIdAsync(campaign.Id, default)).ReturnsAsync(campaign);
        this.campaignRepository.Setup(r => r.UpdateAsync(campaign, default)).Returns(Task.CompletedTask);

        await this.controller.TransitionCampaignStatusAsync(campaign.Id, CampaignStatus.Completed);

        campaign.Status.Should().Be(CampaignStatus.Completed);
        campaign.CompletedAt.Should().NotBeNull();
    }
}
