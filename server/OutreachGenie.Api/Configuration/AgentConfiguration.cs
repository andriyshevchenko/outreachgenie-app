namespace OutreachGenie.Api.Configuration;

/// <summary>
/// Configuration for the agent background service.
/// Binds to AgentSettings section in appsettings.json.
/// </summary>
public sealed class AgentConfiguration
{
    /// <summary>
    /// Gets the interval in milliseconds between campaign polling cycles.
    /// Default: 60000 (60 seconds).
    /// </summary>
    public int PollingIntervalMs { get; init; } = 60000;

    /// <summary>
    /// Gets the maximum number of campaigns that can be processed concurrently.
    /// Default: 1 (avoid LinkedIn rate limits).
    /// </summary>
    public int MaxConcurrentCampaigns { get; init; } = 1;
}
