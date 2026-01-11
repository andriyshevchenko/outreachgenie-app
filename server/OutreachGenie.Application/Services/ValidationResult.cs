namespace OutreachGenie.Application.Services;

/// <summary>
/// Represents validation result with immutable state.
/// Encapsulates validation outcome and error details.
/// Usage: new ValidationResult(true) for success, new ValidationResult(false, "message") for failure.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    /// <param name="valid">Indicates whether validation passed.</param>
    /// <param name="message">Error message if validation failed.</param>
    public ValidationResult(bool valid, string message)
    {
        this.IsValid = valid;
        this.ErrorMessage = message;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class for successful validation.
    /// </summary>
    /// <param name="valid">Indicates whether validation passed.</param>
    public ValidationResult(bool valid)
    {
        this.IsValid = valid;
        this.ErrorMessage = string.Empty;
    }

    /// <summary>
    /// Gets a value indicating whether validation passed.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets validation error message.
    /// </summary>
    public string ErrorMessage { get; }
}
