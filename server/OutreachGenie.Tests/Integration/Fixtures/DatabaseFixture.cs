using Microsoft.EntityFrameworkCore;
using OutreachGenie.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace OutreachGenie.Tests.Integration.Fixtures;

/// <summary>
/// Provides a shared database container for integration tests using Testcontainers.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseFixture"/> class.
    /// </summary>
    public DatabaseFixture()
    {
        this.container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("outreachgenie_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    /// <summary>
    /// Gets the connection string for the test database.
    /// </summary>
    public string ConnectionString => this.container.GetConnectionString();

    /// <summary>
    /// Creates a new DbContext instance for testing.
    /// </summary>
    /// <returns>A configured OutreachGenieDbContext.</returns>
    public OutreachGenieDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OutreachGenieDbContext>()
            .UseNpgsql(this.ConnectionString)
            .Options;

        var context = new OutreachGenieDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>
    /// Starts the database container.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        await this.container.StartAsync();
    }

    /// <summary>
    /// Stops and disposes the database container.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        await this.container.DisposeAsync();
    }
}
