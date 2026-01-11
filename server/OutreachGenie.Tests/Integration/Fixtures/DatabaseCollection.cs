using Xunit;

namespace OutreachGenie.Tests.Integration.Fixtures;

/// <summary>
/// Collection definition for sharing database fixture across integration tests.
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
