using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OutreachGenie.Application.Services;
using Xunit;

namespace OutreachGenie.Tests.Application.Services;

#pragma warning disable SA1600
#pragma warning disable CS1591
public sealed class McpConfigurationLoaderTests
{
    private readonly Mock<ILogger<McpConfigurationLoader>> logger;
    private readonly McpConfigurationLoader loader;
    private readonly string tempDir;

    public McpConfigurationLoaderTests()
    {
        this.logger = new Mock<ILogger<McpConfigurationLoader>>();
        this.loader = new McpConfigurationLoader(this.logger.Object);
        this.tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(this.tempDir);
    }

    [Fact]
    public void LoadReturnsEmptyConfigurationWhenFileDoesNotExist()
    {
        var path = Path.Combine(this.tempDir, "nonexistent.json");

        var config = this.loader.Load(path);

        config.Servers.Should().BeEmpty();
        config.Inputs.Should().BeEmpty();
    }

    [Fact]
    public void LoadParsesValidMcpJsonConfiguration()
    {
        var json = """
        {
            "servers": {
                "exa": {
                    "command": "npx",
                    "args": ["-y", "exa-mcp-server"],
                    "type": "stdio",
                    "env": {
                        "EXA_API_KEY": "test-key"
                    }
                }
            }
        }
        """;
        var path = Path.Combine(this.tempDir, "mcp.json");
        File.WriteAllText(path, json);

        var config = this.loader.Load(path);

        config.Servers.Should().ContainKey("exa");
        var server = config.Servers["exa"];
        server.Command.Should().Be("npx");
        server.Args.Should().Equal("-y", "exa-mcp-server");
        server.Type.Should().Be("stdio");
        server.Env["EXA_API_KEY"].Should().Be("test-key");
    }

    [Fact]
    public void LoadResolvesInputVariablesInArguments()
    {
        var json = """
        {
            "servers": {
                "test": {
                    "command": "npx",
                    "args": ["tool", "${input:org_name}"],
                    "type": "stdio"
                }
            }
        }
        """;
        var path = Path.Combine(this.tempDir, "mcp.json");
        File.WriteAllText(path, json);
        var inputs = new Dictionary<string, string> { ["org_name"] = "contoso" };

        var config = this.loader.Load(path, inputs);

        config.Servers["test"].Args.Should().Equal("tool", "contoso");
    }

    [Fact]
    public void LoadResolvesEnvironmentVariablesInArguments()
    {
        Environment.SetEnvironmentVariable("TEST_MCP_VAR", "test-value");
        var json = """
        {
            "servers": {
                "test": {
                    "command": "npx",
                    "args": ["tool", "${env:TEST_MCP_VAR}"],
                    "type": "stdio"
                }
            }
        }
        """;
        var path = Path.Combine(this.tempDir, "mcp.json");
        File.WriteAllText(path, json);

        var config = this.loader.Load(path);

        config.Servers["test"].Args.Should().Equal("tool", "test-value");
        Environment.SetEnvironmentVariable("TEST_MCP_VAR", null);
    }

    [Fact]
    public void LoadParsesHttpServerConfiguration()
    {
        var json = """
        {
            "servers": {
                "remote": {
                    "type": "http",
                    "url": "https://example.com/mcp"
                }
            }
        }
        """;
        var path = Path.Combine(this.tempDir, "mcp.json");
        File.WriteAllText(path, json);

        var config = this.loader.Load(path);

        config.Servers["remote"].Type.Should().Be("http");
        config.Servers["remote"].Url.Should().Be("https://example.com/mcp");
    }

    [Fact]
    public void LoadParsesDisabledFlagCorrectly()
    {
        var json = """
        {
            "servers": {
                "test": {
                    "command": "npx",
                    "args": ["tool"],
                    "type": "stdio",
                    "disabled": true
                }
            }
        }
        """;
        var path = Path.Combine(this.tempDir, "mcp.json");
        File.WriteAllText(path, json);

        var config = this.loader.Load(path);

        config.Servers["test"].Disabled.Should().BeTrue();
    }

    [Fact]
    public void LoadParsesAutoApproveListCorrectly()
    {
        var json = """
        {
            "servers": {
                "test": {
                    "command": "npx",
                    "args": ["tool"],
                    "type": "stdio",
                    "autoApprove": ["tool1", "tool2"]
                }
            }
        }
        """;
        var path = Path.Combine(this.tempDir, "mcp.json");
        File.WriteAllText(path, json);

        var config = this.loader.Load(path);

        config.Servers["test"].AutoApprove.Should().Equal("tool1", "tool2");
    }
}
