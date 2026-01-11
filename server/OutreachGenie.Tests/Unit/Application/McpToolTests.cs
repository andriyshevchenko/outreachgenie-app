// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

using System.Text.Json;
using FluentAssertions;
using OutreachGenie.Application.Services.Mcp;
using Xunit;

namespace OutreachGenie.Tests.Unit.Application;

/// <summary>
/// Unit tests for McpTool model.
/// </summary>
public sealed class McpToolTests
{
    /// <summary>
    /// Tests that McpTool initializes with required properties.
    /// </summary>
    [Fact]
    public void Constructor_ShouldSetRequiredProperties()
    {
        var schemaJson = "{\"type\":\"object\",\"properties\":{\"path\":{\"type\":\"string\"}}}";
        var schema = JsonDocument.Parse(schemaJson);
        var tool = new McpTool(
            "read_file",
            "Reads content from a file",
            schema);

        tool.Name.Should().Be("read_file");
        tool.Description.Should().Be("Reads content from a file");
        tool.Schema.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that McpTool supports complex schemas.
    /// </summary>
    [Fact]
    public void InputSchema_ShouldSupportComplexStructures()
    {
        var schemaJson = "{\"type\":\"object\",\"properties\":{\"query\":{\"type\":\"string\"},\"maxResults\":{\"type\":\"number\",\"minimum\":1,\"maximum\":100}}}";
        var schema = JsonDocument.Parse(schemaJson);
        var tool = new McpTool("search", "Search tool", schema);

        tool.Schema.RootElement.GetProperty("type").GetString().Should().Be("object");
    }

    /// <summary>
    /// Tests that McpTool can represent file operations.
    /// </summary>
    [Fact]
    public void McpTool_ShouldRepresentFileOperations()
    {
        var schemaJson = "{\"type\":\"object\",\"properties\":{\"path\":{\"type\":\"string\"},\"content\":{\"type\":\"string\"}},\"required\":[\"path\",\"content\"]}";
        var schema = JsonDocument.Parse(schemaJson);
        var writeFile = new McpTool(
            "write_file",
            "Writes content to a file",
            schema);

        writeFile.Name.Should().Be("write_file");
        writeFile.Schema.RootElement.GetProperty("required").GetArrayLength().Should().Be(2);
    }
}
