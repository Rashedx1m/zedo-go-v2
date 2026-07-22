namespace Application;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }
    public string ErrorCode { get; }

    protected Result(bool isSuccess, string error, string errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, string.Empty, string.Empty);
    public static Result Failure(string error, string errorCode = "") => new(false, error, errorCode);

    public static Result<T> Success<T>(T value) => new(value, true, string.Empty, string.Empty);
    public static Result<T> Failure<T>(string error, string errorCode = "") => new(default!, false, error, errorCode);
}

public class Result<T> : Result
{
    public T Value { get; }

    protected internal Result(T value, bool isSuccess, string error, string errorCode)
        : base(isSuccess, error, errorCode)
    {
        Value = value;
    }

    public static implicit operator Result<T>(T value) => Success(value);
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
