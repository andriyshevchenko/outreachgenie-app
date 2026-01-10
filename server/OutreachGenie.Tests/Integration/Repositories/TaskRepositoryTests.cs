using FluentAssertions;
using OutreachGenie.Domain.Entities;
using OutreachGenie.Domain.Enums;
using OutreachGenie.Infrastructure.Persistence.Repositories;
using OutreachGenie.Tests.Integration.Fixtures;
using Xunit;
using TaskStatus = OutreachGenie.Domain.Enums.TaskStatus;

namespace OutreachGenie.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for TaskRepository.
/// </summary>
[Collection("Database")]
public sealed class TaskRepositoryTests
{
    private readonly DatabaseFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The database fixture.</param>
    public TaskRepositoryTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Tests that GetByStatusAsync filters tasks by status.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetByStatusAsync_ShouldReturnTasksWithMatchingStatus()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new TaskRepository(context);
        var campaignRepository = new CampaignRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Status Test Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "Teams",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/status",
        };
        await campaignRepository.CreateAsync(campaign);
        var pendingTask = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Description = "Pending Task",
            Status = TaskStatus.Pending,
            Type = "search",
            CreatedAt = DateTime.UtcNow,
        };
        var inProgressTask = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Description = "In Progress Task",
            Status = TaskStatus.InProgress,
            Type = "analyze",
            CreatedAt = DateTime.UtcNow,
        };
        await repository.CreateAsync(pendingTask);
        await repository.CreateAsync(inProgressTask);

        var pendingTasks = await repository.GetByStatusAsync(campaign.Id, TaskStatus.Pending);

        pendingTasks.Should().Contain(t => t.Id == pendingTask.Id);
        pendingTasks.Should().NotContain(t => t.Id == inProgressTask.Id);
    }

    /// <summary>
    /// Tests that GetByCampaignIdAsync returns tasks ordered by creation time.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GetByCampaignIdAsync_ShouldReturnTasksOrderedByCreation()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new TaskRepository(context);
        var campaignRepository = new CampaignRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Task Test Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "Developers",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/tasks",
        };
        await campaignRepository.CreateAsync(campaign);
        var task1 = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Description = "First Task",
            Status = TaskStatus.Pending,
            Type = "search",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
        };
        var task2 = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Description = "Second Task",
            Status = TaskStatus.Pending,
            Type = "analyze",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
        };
        var task3 = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Description = "Third Task",
            Status = TaskStatus.Pending,
            Type = "send",
            CreatedAt = DateTime.UtcNow,
        };
        await repository.CreateAsync(task1);
        await repository.CreateAsync(task2);
        await repository.CreateAsync(task3);

        var tasks = await repository.GetByCampaignIdAsync(campaign.Id);

        tasks.Should().HaveCountGreaterOrEqualTo(3);
        var campaignTasks = tasks.Where(t => t.CampaignId == campaign.Id).ToList();
        campaignTasks[0].Description.Should().Be("First Task");
        campaignTasks[1].Description.Should().Be("Second Task");
        campaignTasks[2].Description.Should().Be("Third Task");
    }

    /// <summary>
    /// Tests that UpdateAsync correctly modifies task status and metadata.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task UpdateAsync_ShouldModifyTaskStatusAndMetadata()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new TaskRepository(context);
        var campaignRepository = new CampaignRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Update Test Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "QA",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/update",
        };
        await campaignRepository.CreateAsync(campaign);
        var task = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Description = "Test Task",
            Status = TaskStatus.Pending,
            Type = "search",
            CreatedAt = DateTime.UtcNow,
        };
        await repository.CreateAsync(task);
        task.Status = TaskStatus.Done;
        task.OutputJson = "{\"results\":\"success\"}";
        task.CompletedAt = DateTime.UtcNow;

        await repository.UpdateAsync(task);
        var updated = await repository.GetByIdAsync(task.Id);

        updated.Should().NotBeNull();
        updated!.Status.Should().Be(TaskStatus.Done);
        updated.OutputJson.Should().Contain("success");
        updated.CompletedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that task retry logic is properly handled.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task UpdateAsync_ShouldHandleRetryLogic()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new TaskRepository(context);
        var campaignRepository = new CampaignRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Retry Test Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "Support",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/retry",
        };
        await campaignRepository.CreateAsync(campaign);
        var task = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Description = "Retryable Task",
            Status = TaskStatus.Failed,
            Type = "api_call",
            RetryCount = 0,
            MaxRetries = 3,
            ErrorMessage = "Network timeout",
            CreatedAt = DateTime.UtcNow,
        };
        await repository.CreateAsync(task);
        task.RetryCount = 1;
        task.Status = TaskStatus.Retrying;

        await repository.UpdateAsync(task);
        var updated = await repository.GetByIdAsync(task.Id);

        updated.Should().NotBeNull();
        updated!.RetryCount.Should().Be(1);
        updated.Status.Should().Be(TaskStatus.Retrying);
        updated.MaxRetries.Should().Be(3);
    }

    /// <summary>
    /// Tests that CreateAsync properly stores JSON input and output.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CreateAsync_ShouldStoreJsonData()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new TaskRepository(context);
        var campaignRepository = new CampaignRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "JSON Test Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "Managers",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/json",
        };
        await campaignRepository.CreateAsync(campaign);
        var task = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Description = "Task with JSON",
            Status = TaskStatus.Pending,
            Type = "complex_operation",
            InputJson = "{\"query\":\"find CTOs\",\"location\":\"San Francisco\"}",
            CreatedAt = DateTime.UtcNow,
        };

        var created = await repository.CreateAsync(task);
        var retrieved = await repository.GetByIdAsync(created.Id);

        retrieved.Should().NotBeNull();
        retrieved!.InputJson.Should().Contain("find CTOs");
        retrieved.InputJson.Should().Contain("San Francisco");
    }

    /// <summary>
    /// Tests that DeleteAsync removes task.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DeleteAsync_ShouldRemoveTask()
    {
        await using var context = this.fixture.CreateDbContext();
        var repository = new TaskRepository(context);
        var campaignRepository = new CampaignRepository(context);
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            Name = "Delete Test Campaign",
            Status = CampaignStatus.Active,
            TargetAudience = "Interns",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WorkingDirectory = "/test/delete",
        };
        await campaignRepository.CreateAsync(campaign);
        var task = new CampaignTask
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Description = "Task to Delete",
            Status = TaskStatus.Pending,
            Type = "temp",
            CreatedAt = DateTime.UtcNow,
        };
        await repository.CreateAsync(task);

        await repository.DeleteAsync(task.Id);
        var deleted = await repository.GetByIdAsync(task.Id);

        deleted.Should().BeNull();
    }
}
