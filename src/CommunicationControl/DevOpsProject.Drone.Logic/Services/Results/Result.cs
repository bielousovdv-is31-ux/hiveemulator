namespace DevOpsProject.Drone.Logic.Services.Results;

public class Result
{
    public bool IsSuccess { get; protected set; }
    public ErrorType ErrorType { get; protected set; }

    public Result()
    {
        IsSuccess = true;
    }

    public Result(ErrorType errorType)
    {
        ErrorType = errorType;
    }

    public static Result Success() => new Result();

    public static Result Error(ErrorType type) =>
        new Result(type);
    
    public static Result<T> Success<T>(T value) => new Result<T>(value);
    
    public static Result<T> Error<T>(ErrorType type) => new Result<T>(type);
}

public class Result<T> : Result
{
    public T? Value { get; }
    
    public Result(T value) : base()
    {
        Value = value;
    }

    public Result(ErrorType errorType) : base(errorType)
    {
        
    }
}
