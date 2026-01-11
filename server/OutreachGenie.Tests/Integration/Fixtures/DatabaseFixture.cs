// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OutreachGenie.Infrastructure.Persistence;
using Xunit;

namespace OutreachGenie.Tests.Integration.Fixtures;

/// <summary>
/// Provides a shared in-memory SQLite database for integration tests.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private static int databaseCounter;
    private SqliteConnection? connection;

    /// <summary>
    /// Gets unique database name for this test session.
    /// </summary>
    public string DatabaseName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the connection string for the test database.
    /// </summary>
    public string ConnectionString => $"DataSource={this.DatabaseName};Mode=Memory;Cache=Shared";

    /// <summary>
    /// Creates a new DbContext instance for testing.
    /// </summary>
    /// <returns>A configured OutreachGenieDbContext.</returns>
    public OutreachGenieDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OutreachGenieDbContext>()
            .UseSqlite(this.ConnectionString)
            .Options;

        var context = new OutreachGenieDbContext(options);
        return context;
    }

    /// <summary>
    /// Initializes the in-memory database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        var id = Interlocked.Increment(ref databaseCounter);
        this.DatabaseName = $"TestDb_{id}_{Guid.NewGuid():N}";
        this.connection = new SqliteConnection(this.ConnectionString);
        await this.connection.OpenAsync();
        await using var context = this.CreateDbContext();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Closes the database connection.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        if (this.connection != null)
        {
            await this.connection.CloseAsync();
            await this.connection.DisposeAsync();
        }
    }
}
