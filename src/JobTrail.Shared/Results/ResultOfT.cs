namespace JobTrail.Shared.Results;

/// <summary>A <see cref="Result"/> that carries a value when it succeeds.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T value)
        : base(null)
    {
        _value = value;
    }

    internal Result(Error error)
        : base(error)
    {
    }

    /// <summary>The value of a successful result. Reading it on a failure is a programming error.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error.Code}).");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
