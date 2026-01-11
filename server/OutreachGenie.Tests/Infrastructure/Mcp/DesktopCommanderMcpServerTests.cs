// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OutreachGenie.Infrastructure.Mcp;

namespace OutreachGenie.Tests.Infrastructure.Mcp;

/// <summary>
/// Tests for Desktop Commander MCP server implementation.
/// </summary>
public sealed class DesktopCommanderMcpServerTests
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
        var server = new DesktopCommanderMcpServer(transport, NullLogger<DesktopCommanderMcpServer>.Instance);
        await server.ConnectAsync();
        server.IsConnected.Should().BeTrue("server must be connected after initialization");
    }

    /// <summary>
    /// Tests that ListToolsAsync returns available tools from server.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListToolsAsync_ShouldReturnAvailableTools()
    {
        var transport = new FakeMcpTransport();
        transport.AddResponse("initialize", JsonDocument.Parse("{\"result\":{}}"));
        transport.AddResponse("tools/list", JsonDocument.Parse(
            "{\"result\":{\"tools\":[{\"name\":\"read_file\",\"description\":\"Read file content\",\"inputSchema\":{\"type\":\"object\"}}]}}"));
        var server = new DesktopCommanderMcpServer(transport, NullLogger<DesktopCommanderMcpServer>.Instance);
        await server.ConnectAsync();
        var tools = await server.ListToolsAsync();
        tools.Should().HaveCount(1, "server must return one tool");
    }

    /// <summary>
    /// Tests that CallToolAsync executes tool and returns result.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CallToolAsync_ShouldExecuteToolAndReturnResult()
    {
        var transport = new FakeMcpTransport();
        transport.AddResponse("initialize", JsonDocument.Parse("{\"result\":{}}"));
        transport.AddResponse("tools/call", JsonDocument.Parse(
            "{\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"file content\"}]}}"));
        var server = new DesktopCommanderMcpServer(transport, NullLogger<DesktopCommanderMcpServer>.Instance);
        await server.ConnectAsync();
        var parameters = JsonDocument.Parse("{\"path\":\"/test/file.txt\"}");
        var result = await server.CallToolAsync("read_file", parameters);
        result.RootElement.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()
            .Should().Be("file content", "tool must return expected result");
    }

    /// <summary>
    /// Tests that CallToolAsync throws when tool returns error.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task CallToolAsync_ShouldThrowWhenToolReturnsError()
    {
        var transport = new FakeMcpTransport();
        transport.AddResponse("initialize", JsonDocument.Parse("{\"result\":{}}"));
        transport.AddResponse("tools/call", JsonDocument.Parse("{\"error\":{\"message\":\"File not found\"}}"));
        var server = new DesktopCommanderMcpServer(transport, NullLogger<DesktopCommanderMcpServer>.Instance);
        await server.ConnectAsync();
        var parameters = JsonDocument.Parse("{\"path\":\"/nonexistent.txt\"}");
        var action = async () => await server.CallToolAsync("read_file", parameters);
        await action.Should().ThrowAsync<InvalidOperationException>("tool error must be propagated as exception");
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
        var server = new DesktopCommanderMcpServer(transport, NullLogger<DesktopCommanderMcpServer>.Instance);
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
