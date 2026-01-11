// SPDX-FileCopyrightText: Copyright (c) 2025 Yegor Bugayenko
// SPDX-License-Identifier: MIT

namespace OutreachGenie.Application.Services.Llm;

/// <summary>
/// Configuration settings for LLM provider behavior.
/// Controls generation parameters such as temperature, token limits, and retry logic.
/// </summary>
/// <param name="temperature">Sampling temperature (0.0 to 2.0). Lower values are more deterministic.</param>
/// <param name="tokens">Maximum number of tokens to generate.</param>
/// <param name="model">The specific model to use (e.g., "gpt-4", "claude-3-sonnet").</param>
/// <param name="retries">Maximum number of retry attempts on failure.</param>
/// <param name="timeout">Timeout for each API call in seconds.</param>
public sealed class LlmConfiguration(
    double temperature,
    int tokens,
    string model,
    int retries,
    int timeout)
{
    /// <summary>
    /// Gets the sampling temperature for generation.
    /// </summary>
    public double Temperature { get; } = temperature;

    /// <summary>
    /// Gets the maximum number of tokens to generate.
    /// </summary>
    public int MaxTokens { get; } = tokens;

    /// <summary>
    /// Gets the model identifier to use.
    /// </summary>
    public string Model { get; } = model;

    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; } = retries;

    /// <summary>
    /// Gets the timeout in seconds for API calls.
    /// </summary>
    public int TimeoutSeconds { get; } = timeout;
}
