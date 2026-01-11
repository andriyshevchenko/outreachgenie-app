namespace OutreachGenie.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using OutreachGenie.Application.Services.Mcp;

/// <summary>
/// Controller for MCP server diagnostics and testing.
/// </summary>
[ApiController]
[Route("api/v1/mcp")]
public sealed class McpController : ControllerBase
{
    private readonly IMcpToolRegistry registry;
    private readonly ILogger<McpController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpController"/> class.
    /// </summary>
    /// <param name="registry">MCP tool registry.</param>
    /// <param name="logger">Logger instance.</param>
    public McpController(IMcpToolRegistry registry, ILogger<McpController> logger)
    {
        this.registry = registry;
        this.logger = logger;
    }

    /// <summary>
    /// Gets list of connected MCP servers.
    /// </summary>
    /// <returns>Server information.</returns>
    [HttpGet("servers")]
    public IActionResult GetServers()
    {
        var servers = this.registry.All().Select(s => new
        {
            s.Id,
            s.Name,
            s.IsConnected,
        });

        return Ok(new
        {
            Count = servers.Count(),
            Servers = servers,
        });
    }

    /// <summary>
    /// Discovers all available tools from registered MCP servers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available tools.</returns>
    [HttpGet("tools")]
    public async Task<IActionResult> DiscoverTools(CancellationToken cancellationToken)
    {
        try
        {
            var tools = await this.registry.DiscoverToolsAsync(cancellationToken);

            var toolList = tools.Select(t => new
            {
                t.Name,
                t.Description,
            });

            return Ok(new
            {
                Count = tools.Count,
                Tools = toolList,
            });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to discover tools");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Gets basic registry statistics.
    /// </summary>
    /// <returns>Registry statistics.</returns>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        var servers = this.registry.All();

        return Ok(new
        {
            ServerCount = servers.Count(),
            ConnectedServers = servers.Count(s => s.IsConnected),
        });
    }
}
