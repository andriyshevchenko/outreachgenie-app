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

public sealed class PlaywrightMcpIntegrationTests : IDisposable
{
    private readonly StdioMcpTransport transport;
    private readonly PlaywrightMcpServer server;

    public PlaywrightMcpIntegrationTests()
    {
        this.transport = new StdioMcpTransport(
            "npx",
            "@playwright/mcp@latest --headless",
            NullLogger<StdioMcpTransport>.Instance);
        this.server = new PlaywrightMcpServer(this.transport, NullLogger<PlaywrightMcpServer>.Instance);
    }

    [Fact]
    public async Task ConnectAsync_spawns_playwright_mcp_server_successfully()
    {
        await this.server.ConnectAsync();

        this.server.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task ListToolsAsync_returns_playwright_browser_tools()
    {
        await this.server.ConnectAsync();

        var tools = await this.server.ListToolsAsync();

        tools.Should().NotBeEmpty();
        tools.Should().Contain(t => t.Name == "browser_navigate");
        tools.Should().Contain(t => t.Name == "browser_click");
    }

    [Fact]
    public async Task CallToolAsync_navigate_loads_web_page()
    {
        await this.server.ConnectAsync();
        var tools = await this.server.ListToolsAsync();
        tools.Should().NotBeEmpty("server should be initialized with tools");

        var parameters = JsonDocument.Parse("""
        {
            "url": "https://example.com"
        }
        """);

        var result = await this.server.CallToolAsync("browser_navigate", parameters);

        result.RootElement.TryGetProperty("result", out var resultProp).Should().BeTrue();
        resultProp.TryGetProperty("content", out var content).Should().BeTrue();
        content.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CallToolAsync_click_interacts_with_page_elements()
    {
        await this.server.ConnectAsync();

        var navigateParams = JsonDocument.Parse("""
        {
            "url": "https://example.com"
        }
        """);
        await this.server.CallToolAsync("browser_navigate", navigateParams);

        var clickParams = JsonDocument.Parse("""
        {
            "element": "a",
            "ref": "0"
        }
        """);

        var act = async () => await this.server.CallToolAsync("browser_click", clickParams);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CallToolAsync_screenshot_captures_page_image()
    {
        await this.server.ConnectAsync();
        var tools = await this.server.ListToolsAsync();
        tools.Should().NotBeEmpty("server should be initialized with tools");

        var navigateParams = JsonDocument.Parse("""
        {
            "url": "https://example.com"
        }
        """);
        await this.server.CallToolAsync("browser_navigate", navigateParams);

        var screenshotParams = JsonDocument.Parse("{}");

        var result = await this.server.CallToolAsync("browser_screenshot", screenshotParams);

        result.RootElement.TryGetProperty("result", out var resultProp).Should().BeTrue();
        resultProp.TryGetProperty("content", out var content).Should().BeTrue();
        content.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DisconnectAsync_closes_browser_and_server()
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
