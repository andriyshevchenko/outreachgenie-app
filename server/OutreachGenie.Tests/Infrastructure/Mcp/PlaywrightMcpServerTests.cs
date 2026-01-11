// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OutreachGenie.Infrastructure.Mcp;

namespace OutreachGenie.Tests.Infrastructure.Mcp;

/// <summary>
/// Tests for Playwright MCP server implementation.
/// </summary>
public sealed class PlaywrightMcpServerTests
{
    /// <summary>
    /// Tests that ConnectAsync initializes server with protocol handshake.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ConnectAsync_ShouldInitializeServerWithProtocolHandshake()
    {
        var transport = new FakeMcpTransport();
        transport.AddResponse("initialize", JsonDocument.Parse("{\"result\":{\"protocolVersion\":\"2024-11-05\"}}"));
        var server = new PlaywrightMcpServer(transport, NullLogger<PlaywrightMcpServer>.Instance);
        await server.ConnectAsync();
        server.IsConnected.Should().BeTrue("server must be connected after initialization");
    }

    /// <summary>
    /// Tests that ListToolsAsync returns available browser automation tools.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListToolsAsync_ShouldReturnBrowserAutomationTools()
    {
        var transport = new FakeMcpTransport();
        transport.AddResponse("initialize", JsonDocument.Parse("{\"result\":{}}"));
        transport.AddResponse("tools/list", JsonDocument.Parse(
            "{\"result\":{\"tools\":[{\"name\":\"playwright_navigate\",\"description\":\"Navigate to URL\",\"inputSchema\":{\"type\":\"object\"}}]}}"));
        var server = new PlaywrightMcpServer(transport, NullLogger<PlaywrightMcpServer>.Instance);
        await server.ConnectAsync();
        var tools = await server.ListToolsAsync();
        tools.Should().HaveCount(1, "server must return one tool");
    }

    /// <summary>
    /// Tests that CallToolAsync executes browser action and returns result.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CallToolAsync_ShouldExecuteBrowserActionAndReturnResult()
    {
        var transport = new FakeMcpTransport();
        transport.AddResponse("initialize", JsonDocument.Parse("{\"result\":{}}"));
        transport.AddResponse("tools/call", JsonDocument.Parse(
            "{\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"Navigation successful\"}]}}"));
        var server = new PlaywrightMcpServer(transport, NullLogger<PlaywrightMcpServer>.Instance);
        await server.ConnectAsync();
        var parameters = JsonDocument.Parse("{\"url\":\"https://example.com\"}");
        var result = await server.CallToolAsync("playwright_navigate", parameters);
        result.RootElement.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()
            .Should().Be("Navigation successful", "tool must return expected result");
    }

    /// <summary>
    /// Tests that CallToolAsync throws when browser action fails.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CallToolAsync_ShouldThrowWhenBrowserActionFails()
    {
        var transport = new FakeMcpTransport();
        transport.AddResponse("initialize", JsonDocument.Parse("{\"result\":{}}"));
        transport.AddResponse("tools/call", JsonDocument.Parse("{\"error\":{\"message\":\"Navigation timeout\"}}"));
        var server = new PlaywrightMcpServer(transport, NullLogger<PlaywrightMcpServer>.Instance);
        await server.ConnectAsync();
        var parameters = JsonDocument.Parse("{\"url\":\"https://invalid.com\"}");
        var action = async () => await server.CallToolAsync("playwright_navigate", parameters);
        await action.Should().ThrowAsync<InvalidOperationException>("browser error must be propagated as exception");
    }

    /// <summary>
    /// Tests that DisconnectAsync closes browser and transport connection.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DisconnectAsync_ShouldCloseBrowserAndTransport()
    {
        var transport = new FakeMcpTransport();
        transport.AddResponse("initialize", JsonDocument.Parse("{\"result\":{}}"));
        var server = new PlaywrightMcpServer(transport, NullLogger<PlaywrightMcpServer>.Instance);
        await server.ConnectAsync();
        await server.DisconnectAsync();
        server.IsConnected.Should().BeFalse("server must be disconnected after disconnect call");
    }

    private sealed class FakeMcpTransport : OutreachGenie.Application.Interfaces.Mcp.IMcpTransport
    {
        private readonly Dictionary<string, JsonDocument> responses = new();
        private bool connected;

        public bool IsConnected => this.connected;

        public void AddResponse(string method, JsonDocument response)
        {
            this.responses[method] = response;
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            this.connected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            this.connected = false;
            return Task.CompletedTask;
        }

        public Task<JsonDocument> SendAsync(JsonDocument request, CancellationToken cancellationToken = default)
        {
            var method = request.RootElement.GetProperty("method").GetString() ?? string.Empty;
            return Task.FromResult(this.responses[method]);
        }
    }
}
