// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json;

namespace OutreachGenie.Application.Services.Mcp;

/// <summary>
/// Represents a tool exposed by an MCP server.
/// Contains tool metadata including name, description, and JSON schema for parameters.
/// </summary>
/// <param name="name">Unique identifier of the tool.</param>
/// <param name="description">Human-readable description of what the tool does.</param>
/// <param name="schema">JSON schema defining the tool's input parameters.</param>
public sealed class McpTool(string name, string description, JsonDocument schema)
{
    /// <summary>
    /// Gets the unique identifier of this tool.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the description explaining the tool's purpose and behavior.
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    /// Gets the JSON schema that defines valid input parameters for this tool.
    /// </summary>
    public JsonDocument Schema { get; } = schema;
}
