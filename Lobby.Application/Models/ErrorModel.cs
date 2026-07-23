namespace Lobby.Application.Models;

public class ErrorModel(string? errorMessage, ErrorType errorType)
{
    public string? ErrorMessage { get; } = errorMessage;
    public ErrorType ErrorType { get; } = errorType;
    public static ErrorModel NotFound(string? message = "Given element not found") => new(message, ErrorType.NotFound);
    public static ErrorModel Conflict(string? message = "Conflict operation") => new(message, ErrorType.Conflict);
    public static ErrorModel Validation(string? message = "Invalid arguments were passed") => new(message, ErrorType.Validation);
    public static ErrorModel Unauthorized(string? message = "Unauthorized access") => new(message, ErrorType.Unauthorized);
    public static ErrorModel UnexpectedError(string? message = "Failed to do the operation") => new(message, ErrorType.UnexpectedError);
    public static ErrorModel ResourceExhausted(string? message = "Maximum server capacity reached. No available container slots.") => new(message, ErrorType.ResourceExhausted);
}

public enum ErrorType
{
    NotFound,
    Conflict,
    Validation,
    Unauthorized,
    UnexpectedError,
    ResourceExhausted
}