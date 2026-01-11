namespace OutreachGenie.Application.Services;

/// <summary>
/// Represents validation result.
/// </summary>
public sealed class ValidationResult
{
    private ValidationResult(bool isValid, string? errorMessage = null)
    {
        this.IsValid = isValid;
        this.ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets a value indicating whether validation passed.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets validation error message.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates successful validation result.
    /// </summary>
    /// <returns>Success result.</returns>
    public static ValidationResult Success() => new(true);

    /// <summary>
    /// Creates failed validation result.
    /// </summary>
    /// <param name="errorMessage">Error message.</param>
    /// <returns>Failure result.</returns>
    public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
}
