// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OutreachGenie.Infrastructure.Mcp;

namespace OutreachGenie.Tests.Infrastructure.Mcp;

/// <summary>
/// Tests for Exa MCP server implementation.
/// </summary>
public sealed class ExaMcpServerTests
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
        var server = new ExaMcpServer(transport, NullLogger<ExaMcpServer>.Instance);
        await server.ConnectAsync();
        server.IsConnected.Should().BeTrue("server must be connected after initialization");
    }

    /// <summary>
    /// Tests that ListToolsAsync returns web search tools.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListToolsAsync_ShouldReturnWebSearchTools()
    {
        var transport = new FakeMcpTransport();
        transport.AddResponse("initialize", JsonDocument.Parse("{\"result\":{}}"));
        transport.AddResponse("tools/list", JsonDocument.Parse(
            "{\"result\":{\"tools\":[{\"name\":\"exa_search\",\"description\":\"Search web\",\"inputSchema\":{\"type\":\"object\"}}]}}"));
        var server = new ExaMcpServer(transport, NullLogger<ExaMcpServer>.Instance);
        await server.ConnectAsync();
        var tools = await server.ListToolsAsync();
        tools.Should().HaveCount(1, "server must return one tool");
    }

    /// <summary>
    /// Tests that CallToolAsync executes search and returns results.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CallToolAsync_ShouldExecuteSearchAndReturnResults()
    {
        var transport = new FakeMcpTransport();
        transport.AddResponse("initialize", JsonDocument.Parse("{\"result\":{}}"));
        transport.AddResponse("tools/call", JsonDocument.Parse(
            "{\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"Search results\"}]}}"));
        var server = new ExaMcpServer(transport, NullLogger<ExaMcpServer>.Instance);
        await server.ConnectAsync();
        var parameters = JsonDocument.Parse("{\"query\":\"test query\"}");
        var result = await server.CallToolAsync("exa_search", parameters);
        result.RootElement.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()
            .Should().Be("Search results", "tool must return search results");
    }

    /// <summary>
    /// Tests that CallToolAsync throws when search fails.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CallToolAsync_ShouldThrowWhenSearchFails()
    {
        var transport = new FakeMcpTransport();
        transport.AddResponse("initialize", JsonDocument.Parse("{\"result\":{}}"));
        transport.AddResponse("tools/call", JsonDocument.Parse("{\"error\":{\"message\":\"API error\"}}"));
        var server = new ExaMcpServer(transport, NullLogger<ExaMcpServer>.Instance);
        await server.ConnectAsync();
        var parameters = JsonDocument.Parse("{\"query\":\"test\"}");
        var action = async () => await server.CallToolAsync("exa_search", parameters);
        await action.Should().ThrowAsync<InvalidOperationException>("search error must be propagated as exception");
    }

    /// <summary>
    /// Tests that DisconnectAsync closes transport connection.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DisconnectAsync_ShouldCloseTransportConnection()
    {
        var transport = new FakeMcpTransport();
        transport.AddResponse("initialize", JsonDocument.Parse("{\"result\":{}}"));
        var server = new ExaMcpServer(transport, NullLogger<ExaMcpServer>.Instance);
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
