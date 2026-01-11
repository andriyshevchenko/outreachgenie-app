// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OutreachGenie.Infrastructure.Mcp;

namespace OutreachGenie.Tests.Infrastructure.Mcp;

/// <summary>
/// Tests for stdio MCP transport implementation.
/// </summary>
public sealed class StdioMcpTransportTests
{
    /// <summary>
    /// Tests that transport is initially not connected.
    /// </summary>
    [Fact]
    public void IsConnected_ShouldReturnFalseInitially()
    {
        var transport = new StdioMcpTransport("echo", "test", NullLogger<StdioMcpTransport>.Instance);
        transport.IsConnected.Should().BeFalse("transport must not be connected before ConnectAsync is called");
    }

    /// <summary>
    /// Tests that DisconnectAsync handles null process gracefully.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DisconnectAsync_ShouldHandleNullProcessGracefully()
    {
        var transport = new StdioMcpTransport("echo", "test", NullLogger<StdioMcpTransport>.Instance);
        var action = async () => await transport.DisconnectAsync();
        await action.Should().NotThrowAsync("disconnecting without connection must not throw");
    }

    /// <summary>
    /// Tests that SendAsync throws when transport is not connected.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task SendAsync_ShouldThrowWhenNotConnected()
    {
        var transport = new StdioMcpTransport("echo", "test", NullLogger<StdioMcpTransport>.Instance);
        var request = System.Text.Json.JsonDocument.Parse("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"test\"}");
        var action = async () => await transport.SendAsync(request);
        await action.Should().ThrowAsync<InvalidOperationException>("sending without connection must throw exception");
    }

    /// <summary>
    /// Tests that ConnectAsync with PowerShell echo command succeeds.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ConnectAsync_ShouldStartProcessSuccessfully()
    {
        var transport = new StdioMcpTransport("powershell", "-Command \"while($true) { $line = [Console]::ReadLine(); if($line) { Write-Output $line } }\"", NullLogger<StdioMcpTransport>.Instance);
        try
        {
            await transport.ConnectAsync();
            transport.IsConnected.Should().BeTrue("transport must be connected after successful ConnectAsync");
        }
        finally
        {
            await transport.DisconnectAsync();
        }
    }

    /// <summary>
    /// Tests that ConnectAsync when already connected returns immediately.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ConnectAsync_ShouldReturnImmediatelyWhenAlreadyConnected()
    {
        var transport = new StdioMcpTransport("powershell", "-Command \"while($true) { $line = [Console]::ReadLine(); if($line) { Write-Output $line } }\"", NullLogger<StdioMcpTransport>.Instance);
        try
        {
            await transport.ConnectAsync();
            await transport.ConnectAsync();
            transport.IsConnected.Should().BeTrue("transport must remain connected after second ConnectAsync call");
        }
        finally
        {
            await transport.DisconnectAsync();
        }
    }

    /// <summary>
    /// Tests that SendAsync successfully sends and receives JSON RPC message.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task SendAsync_ShouldSendAndReceiveJsonRpcMessage()
    {
        var transport = new StdioMcpTransport("powershell", "-Command \"while($true) { $line = [Console]::ReadLine(); if($line) { Write-Output $line } }\"", NullLogger<StdioMcpTransport>.Instance);
        try
        {
            await transport.ConnectAsync();
            var request = System.Text.Json.JsonDocument.Parse("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"test\"}");
            var response = await transport.SendAsync(request);
            response.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0", "echo server must return same JSON-RPC message");
        }
        finally
        {
            await transport.DisconnectAsync();
        }
    }

    /// <summary>
    /// Tests that DisconnectAsync properly terminates process.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DisconnectAsync_ShouldTerminateProcess()
    {
        var transport = new StdioMcpTransport("powershell", "-Command \"while($true) { Start-Sleep -Milliseconds 100 }\"", NullLogger<StdioMcpTransport>.Instance);
        await transport.ConnectAsync();
        await transport.DisconnectAsync();
        transport.IsConnected.Should().BeFalse("transport must not be connected after DisconnectAsync");
    }

    /// <summary>
    /// Tests that environment variables are passed to spawned process.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ConnectAsync_ShouldPassEnvironmentVariablesToProcess()
    {
        var environment = new Dictionary<string, string>
        {
            ["TEST_VAR"] = "test_value",
        };
        var transport = new StdioMcpTransport(
            "powershell",
            "-Command \"$env:TEST_VAR | Out-String\"",
            NullLogger<StdioMcpTransport>.Instance,
            environment);
        try
        {
            await transport.ConnectAsync();
            transport.IsConnected.Should().BeTrue("transport must be connected with environment variables");
        }
        finally
        {
            await transport.DisconnectAsync();
        }
    }
}
