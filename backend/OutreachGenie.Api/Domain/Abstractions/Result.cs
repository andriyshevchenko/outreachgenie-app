namespace OutreachGenie.Api.Domain.Abstractions;

/// <summary>
/// Represents an operation result that can be either success or failure.
/// </summary>
/// <typeparam name="T">The type of the value when successful.</typeparam>
public sealed class Result<T> : IResult<T>
{
    private readonly T? _value;
    private readonly string _error;

    private Result(T value)
    {
        this._value = value;
        this._error = string.Empty;
        this.IsSuccess = true;
    }

    private Result(string error)
    {
        this._value = default;
        this._error = error;
        this.IsSuccess = false;
    }

    /// <inheritdoc />
    public bool IsSuccess { get; }

    /// <inheritdoc />
    public string Error => this._error;

    /// <inheritdoc />
    public T Value =>
        this.IsSuccess
            ? this._value!
            : throw new InvalidOperationException("Cannot access Value of a failed result");

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static Result<T> Failure(string error) => new(error);
}
