namespace Lobby.Application.Models;

public class ResultModel
{
    public bool IsSuccessful { get; }
    public ErrorModel? Error { get; }
    
    private ResultModel(bool isSuccessful, ErrorModel? error)
    {
        IsSuccessful = isSuccessful;
        Error = error;
    }
    public static ResultModel Success() => new(true, null);
    public static ResultModel Failure(ErrorModel error) => new(false, error);

    public static implicit operator ResultModel(ErrorModel error) => Failure(error);
}

public class ResultModel<T>
{
    public bool IsSuccessful { get; }
    public T? Result { get; }
    public ErrorModel? Error { get; }
    
    private ResultModel(bool isSuccessful, T? result, ErrorModel? error)
    {
        IsSuccessful = isSuccessful;
        Result = result;
        Error = error;
    }

    public static ResultModel<T> Success(T value) => new(true, value, null);
    public static ResultModel<T> Failure(ErrorModel error) => new(false, default, error);

    public static implicit operator ResultModel<T>(ErrorModel error) => Failure(error);
    public static implicit operator ResultModel<T>(T value) => Success(value);
}