namespace OutreachGenie.Tests.Infrastructure.Mcp;

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OutreachGenie.Infrastructure.Mcp;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591

public sealed class DesktopCommanderMcpIntegrationTests : IDisposable
{
    private static bool setupCompleted;

    private readonly StdioMcpTransport transport;
    private readonly DesktopCommanderMcpServer server;
    private readonly string tempDir;

    public DesktopCommanderMcpIntegrationTests()
    {
        if (!setupCompleted)
        {
            RunSetup();
            setupCompleted = true;
        }

        this.tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(this.tempDir);

        this.transport = new StdioMcpTransport(
            "npx",
            $"@wonderwhy-er/desktop-commander@latest --working-directory \"{this.tempDir}\"",
            NullLogger<StdioMcpTransport>.Instance);
        this.server = new DesktopCommanderMcpServer(this.transport, NullLogger<DesktopCommanderMcpServer>.Instance);
    }

    [Fact]
    public async Task ConnectAsync_spawns_desktop_commander_successfully()
    {
        await this.server.ConnectAsync();

        this.server.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task ListToolsAsync_returns_file_operation_tools()
    {
        await this.server.ConnectAsync();

        var tools = await this.server.ListToolsAsync();

        tools.Should().NotBeEmpty();
        tools.Should().Contain(t => t.Name == "read_file");
        tools.Should().Contain(t => t.Name == "write_file");
        tools.Should().Contain(t => t.Name == "list_directory");
    }

    [Fact]
    public async Task CallToolAsync_write_file_creates_new_file()
    {
        await this.server.ConnectAsync();

        var testFilePath = Path.Combine(this.tempDir, "test.txt");
        var parameters = JsonDocument.Parse($$"""
        {
            "path": "{{testFilePath.Replace("\\", "\\\\")}}",
            "content": "Hello from Desktop Commander!"
        }
        """);

        await this.server.CallToolAsync("write_file", parameters);

        File.Exists(testFilePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(testFilePath);
        content.Should().Be("Hello from Desktop Commander!");
    }

    [Fact]
    public async Task CallToolAsync_read_file_retrieves_file_content()
    {
        await this.server.ConnectAsync();
        var tools = await this.server.ListToolsAsync();
        tools.Should().NotBeEmpty("server should be initialized with tools");

        var testFilePath = Path.Combine(this.tempDir, "read-test.txt");
        await File.WriteAllTextAsync(testFilePath, "Test content for reading");

        var parameters = JsonDocument.Parse($$"""
        {
            "path": "{{testFilePath.Replace("\\", "\\\\")}}"
        }
        """);

        var result = await this.server.CallToolAsync("read_file", parameters);

        result.RootElement.TryGetProperty("result", out var resultProp).Should().BeTrue();
        resultProp.TryGetProperty("content", out var content).Should().BeTrue();
        content.GetArrayLength().Should().BeGreaterThan(0);
        var contentText = content[0].GetProperty("text").GetString();
        contentText.Should().Contain("Test content for reading");
    }

    [Fact]
    public async Task CallToolAsync_list_directory_shows_files()
    {
        await this.server.ConnectAsync();
        var tools = await this.server.ListToolsAsync();
        tools.Should().NotBeEmpty("server should be initialized with tools");

        await File.WriteAllTextAsync(Path.Combine(this.tempDir, "file1.txt"), "content1");
        await File.WriteAllTextAsync(Path.Combine(this.tempDir, "file2.txt"), "content2");
        Directory.CreateDirectory(Path.Combine(this.tempDir, "subdir"));

        var parameters = JsonDocument.Parse($$"""
        {
            "path": "{{this.tempDir.Replace("\\", "\\\\")}}"
        }
        """);

        var result = await this.server.CallToolAsync("list_directory", parameters);

        result.RootElement.TryGetProperty("result", out var resultProp).Should().BeTrue();
        resultProp.TryGetProperty("content", out var content).Should().BeTrue();
        content.GetArrayLength().Should().BeGreaterThan(0);
        var contentText = content[0].GetProperty("text").GetString();
        contentText.Should().Contain("file1.txt");
        contentText.Should().Contain("file2.txt");
        contentText.Should().Contain("subdir");
    }

    [Fact]
    public async Task CallToolAsync_start_process_runs_shell_commands()
    {
        await this.server.ConnectAsync();
        var tools = await this.server.ListToolsAsync();
        tools.Should().NotBeEmpty("server should be initialized with tools");
        tools.Should().Contain(t => t.Name == "start_process", "desktop commander should have start_process tool");

        var parameters = JsonDocument.Parse("""
        {
            "command": "echo Hello World",
            "timeout_ms": 5000
        }
        """);

        var result = await this.server.CallToolAsync("start_process", parameters);

        result.RootElement.TryGetProperty("result", out var resultProp).Should().BeTrue();
        resultProp.TryGetProperty("content", out var content).Should().BeTrue();
        content.GetArrayLength().Should().BeGreaterThan(0);
        var contentText = content[0].GetProperty("text").GetString();
        contentText.Should().Contain("Hello");
        contentText.Should().Contain("World");
    }

    [Fact]
    public async Task Server_successfully_writes_files_in_working_directory()
    {
        await this.server.ConnectAsync();
        var tools = await this.server.ListToolsAsync();
        tools.Should().NotBeEmpty("server should be initialized with tools");

        var testFilePath = Path.Combine(this.tempDir, "allowed-file.txt");
        var parameters = JsonDocument.Parse($$"""
        {
            "path": "{{testFilePath.Replace("\\", "\\\\")}}",
            "content": "This should succeed"
        }
        """);

        await this.server.CallToolAsync("write_file", parameters);

        File.Exists(testFilePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(testFilePath);
        content.Should().Be("This should succeed");
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

        if (Directory.Exists(this.tempDir))
        {
            Directory.Delete(this.tempDir, true);
        }
    }

    private static void RunSetup()
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "npx",
                Arguments = "@wonderwhy-er/desktop-commander@latest setup",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        process.Start();
        process.WaitForExit(30000);
    }
}
