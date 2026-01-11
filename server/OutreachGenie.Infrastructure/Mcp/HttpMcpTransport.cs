using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutreachGenie.Application.Interfaces.Mcp;

namespace OutreachGenie.Infrastructure.Mcp;

/// <summary>
/// HTTP-based MCP transport for remote server communication.
/// </summary>
/// <remarks>
/// Implements Model Context Protocol communication over HTTP/HTTPS for remote MCP servers.
/// Uses JSON-RPC 2.0 protocol over HTTP POST requests. Supports MCP servers hosted as
/// web services (e.g., Microsoft Docs MCP, YouTube MCP on smithery.ai).
/// </remarks>
public sealed class HttpMcpTransport : IMcpTransport
{
    private readonly HttpClient client;
    private readonly ILogger<HttpMcpTransport> logger;
    private int messageId;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpMcpTransport"/> class.
    /// </summary>
    /// <param name="client">HTTP client for making requests.</param>
    /// <param name="logger">Logger instance.</param>
    public HttpMcpTransport(HttpClient client, ILogger<HttpMcpTransport> logger)
    {
        this.client = client;
        this.logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether the HTTP transport is connected.
    /// </summary>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// Connects to the HTTP MCP server.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel operation.</param>
    /// <returns>Task representing connection operation.</returns>
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        this.IsConnected = true;
        this.logger.LogInformation("HTTP MCP transport initialized for {BaseAddress}", this.client.BaseAddress);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disconnects from the HTTP MCP server.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel operation.</param>
    /// <returns>Task representing disconnection operation.</returns>
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        this.IsConnected = false;
        this.logger.LogInformation("HTTP MCP transport closed");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sends a JSON-RPC request to the HTTP server.
    /// </summary>
    /// <param name="request">JSON-RPC request document.</param>
    /// <param name="cancellationToken">Token to cancel operation.</param>
    /// <returns>JSON response document.</returns>
    public async Task<JsonDocument> SendAsync(JsonDocument request, CancellationToken cancellationToken = default)
    {
        var id = Interlocked.Increment(ref this.messageId);
        var method = request.RootElement.GetProperty("method").GetString();
        var parameters = request.RootElement.TryGetProperty("params", out var paramsElement) ? paramsElement : (object?)null;

        var requestObject = new
        {
            jsonrpc = "2.0",
            id,
            method,
            @params = parameters,
        };

        var json = JsonSerializer.Serialize(requestObject);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        this.logger.LogDebug("Sending HTTP request: {Method}", method);
        var response = await this.client.PostAsync(string.Empty, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        this.logger.LogDebug("Received HTTP response: {Length} bytes", responseJson.Length);

        return JsonDocument.Parse(responseJson);
    }
}
