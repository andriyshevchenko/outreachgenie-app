namespace OutreachGenie.Tests.Infrastructure.Mcp;

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OutreachGenie.Infrastructure.Mcp;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591

public sealed class FetchMcpIntegrationTests : IDisposable
{
    private readonly StdioMcpTransport transport;
    private readonly FetchMcpServer server;

    public FetchMcpIntegrationTests()
    {
        this.transport = new StdioMcpTransport(
            "npx",
            "mcp-fetch-server",
            NullLogger<StdioMcpTransport>.Instance);
        this.server = new FetchMcpServer(this.transport, NullLogger<FetchMcpServer>.Instance);
    }

    [Fact]
    public async Task ConnectAsync_spawns_fetch_mcp_server_successfully()
    {
        await this.server.ConnectAsync();

        this.server.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task ListToolsAsync_returns_fetch_tools()
    {
        await this.server.ConnectAsync();

        var tools = await this.server.ListToolsAsync();

        tools.Should().NotBeEmpty();
        tools.Should().Contain(t => t.Name == "fetch_html" || t.Name == "fetch");
    }

    [Fact]
    public async Task CallToolAsync_fetch_html_retrieves_web_content()
    {
        await this.server.ConnectAsync();
        var tools = await this.server.ListToolsAsync();
        tools.Should().NotBeEmpty("server should be initialized with tools");

        var parameters = JsonDocument.Parse("""
        {
            "url": "https://example.com"
        }
        """);

        var result = await this.server.CallToolAsync("fetch_html", parameters);

        result.RootElement.TryGetProperty("result", out var resultProp).Should().BeTrue();
        resultProp.TryGetProperty("content", out var content).Should().BeTrue();
        content.GetArrayLength().Should().BeGreaterThan(0);
        var contentText = content[0].GetProperty("text").GetString();
        contentText.Should().Contain("Example Domain");
    }

    [Fact]
    public async Task CallToolAsync_fetch_with_start_index_returns_partial_content()
    {
        await this.server.ConnectAsync();
        var tools = await this.server.ListToolsAsync();
        tools.Should().NotBeEmpty("server should be initialized with tools");

        var parameters = JsonDocument.Parse("""
        {
            "url": "https://example.com",
            "start_index": 100,
            "max_length": 50
        }
        """);

        var result = await this.server.CallToolAsync("fetch_html", parameters);

        result.RootElement.TryGetProperty("result", out var resultProp).Should().BeTrue();
        resultProp.TryGetProperty("content", out var content).Should().BeTrue();
        content.GetArrayLength().Should().BeGreaterThan(0);
        var contentText = content[0].GetProperty("text").GetString();
        contentText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Server_handles_invalid_url_gracefully()
    {
        await this.server.ConnectAsync();
        var tools = await this.server.ListToolsAsync();
        tools.Should().NotBeEmpty("server should be initialized with tools");

        var parameters = JsonDocument.Parse("""
        {
            "url": "https://this-domain-does-not-exist-12345.com"
        }
        """);

        var result = await this.server.CallToolAsync("fetch_html", parameters);

        result.RootElement.TryGetProperty("result", out var resultProp).Should().BeTrue();
        resultProp.TryGetProperty("isError", out var isError);
        if (isError.ValueKind != System.Text.Json.JsonValueKind.Undefined)
        {
            isError.GetBoolean().Should().BeTrue();
        }
    }

    [Fact]
    public async Task DisconnectAsync_closes_server_process()
    {
        await this.server.ConnectAsync();

        await this.server.DisconnectAsync();

        this.server.IsConnected.Should().BeFalse();
    }

    public void Dispose()
    {
        if (this.server.IsConnected)
        {
            this.server.DisconnectAsync().GetAwaiter().GetResult();
        }
    }
}
