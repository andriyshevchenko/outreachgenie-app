namespace OutreachGenie.Api.Domain.Abstractions;

/// <summary>
/// Represents a result that indicates success or failure.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public interface IResult<out T>
{
    /// <summary>
    /// Indicates whether the operation succeeded.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// The error message if the operation failed.
    /// </summary>
    string Error { get; }

    /// <summary>
    /// The value if the operation succeeded.
    /// </summary>
    T Value { get; }
}
