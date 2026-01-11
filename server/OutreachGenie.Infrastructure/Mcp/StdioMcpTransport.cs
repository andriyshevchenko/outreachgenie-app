// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OutreachGenie.Application.Interfaces.Mcp;

namespace OutreachGenie.Infrastructure.Mcp;

/// <summary>
/// MCP transport that communicates via standard input/output streams with a subprocess.
/// Used for MCP servers implemented as command-line programs (e.g., Node.js scripts).
/// </summary>
public sealed class StdioMcpTransport : IMcpTransport
{
    private readonly string command;
    private readonly string arguments;
    private readonly ILogger<StdioMcpTransport> logger;
    private readonly SemaphoreSlim sendLock = new(1, 1);
    private readonly Dictionary<string, string>? environment;
    private Process? process;
    private StreamWriter? stdin;
    private StreamReader? stdout;

    /// <summary>
    /// Initializes a new instance of the <see cref="StdioMcpTransport"/> class.
    /// </summary>
    /// <param name="command">The executable command to run.</param>
    /// <param name="arguments">Arguments to pass to the command.</param>
    /// <param name="logger">Logger for transport operations.</param>
    /// <param name="environment">Optional environment variables to set for the process.</param>
    public StdioMcpTransport(
        string command,
        string arguments,
        ILogger<StdioMcpTransport> logger,
        Dictionary<string, string>? environment = null)
    {
        this.command = command;
        this.arguments = arguments;
        this.logger = logger;
        this.environment = environment;
    }

    /// <inheritdoc/>
    public bool IsConnected => this.process != null && !this.process.HasExited;

    /// <inheritdoc/>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            return;
        }

        this.logger.LogInformation("Starting MCP server process: {Command} {Arguments}", this.command, this.arguments);

        this.process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = this.command,
                Arguments = this.arguments,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            },
        };

        if (this.environment != null)
        {
            foreach (var (key, value) in this.environment)
            {
                this.process.StartInfo.EnvironmentVariables[key] = value;
            }
        }

        this.process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                this.logger.LogWarning("MCP server stderr: {ErrorData}", e.Data);
            }
        };

        if (!this.process.Start())
        {
            throw new InvalidOperationException($"Failed to start MCP server process: {this.command}");
        }

        this.process.BeginErrorReadLine();
        this.stdin = this.process.StandardInput;
        this.stdout = this.process.StandardOutput;

        await Task.Delay(500, cancellationToken);

        this.logger.LogInformation("MCP server process started successfully");
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (this.process == null)
        {
            return;
        }

        this.logger.LogInformation("Stopping MCP server process");

        try
        {
            this.stdin?.Close();
            this.stdout?.Close();

            if (!this.process.HasExited)
            {
                this.process.Kill(true);
                await this.process.WaitForExitAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Error while stopping MCP server process");
        }
        finally
        {
            this.process.Dispose();
            this.process = null;
            this.stdin = null;
            this.stdout = null;
        }
    }

    /// <inheritdoc/>
    public async Task<JsonDocument> SendAsync(JsonDocument request, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || this.stdin == null || this.stdout == null)
        {
            throw new InvalidOperationException("Transport is not connected");
        }

        await this.sendLock.WaitAsync(cancellationToken);
        try
        {
            var requestJson = JsonSerializer.Serialize(request);
            this.logger.LogDebug("Sending MCP request: {Request}", requestJson);

            await this.stdin.WriteLineAsync(requestJson.AsMemory(), cancellationToken);
            await this.stdin.FlushAsync();

            var responseLine = await this.stdout.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(responseLine))
            {
                throw new InvalidOperationException("MCP server returned empty response");
            }

            this.logger.LogDebug("Received MCP response: {Response}", responseLine);

            return JsonDocument.Parse(responseLine);
        }
        finally
        {
            this.sendLock.Release();
        }
    }
}
