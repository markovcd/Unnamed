namespace Tally.Core;

/// <summary>A value, or the reason there is none.</summary>
internal readonly struct Result<T>
{
    private readonly T? value;
    private readonly string? error;

    private Result(T? value, string? error)
    {
        this.value = value;
        this.error = error;
    }

    public bool IsOk => error is null;

    public T Value => IsOk
        ? value!
        : throw new InvalidOperationException("A failed result has no value.");

    public string Error => error
        ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result<T> Ok(T value) => new(value, null);

    public static Result<T> Fail(string error) => new(default, error);
}
