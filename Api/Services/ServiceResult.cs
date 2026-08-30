namespace GoodDeedsApi.Services;

public enum ServiceError
{
    None = 0,
    NotFound,
    Conflict,
    Validation
}

/// <summary>
/// Lets services report why something failed without throwing, so controllers
/// can map the outcome onto a status code in one place.
/// </summary>
public readonly record struct ServiceResult<T>(T? Value, ServiceError Error, string? Message)
{
    public bool Succeeded => Error == ServiceError.None;

    public static ServiceResult<T> Ok(T value) => new(value, ServiceError.None, null);
    public static ServiceResult<T> NotFound(string message) => new(default, ServiceError.NotFound, message);
    public static ServiceResult<T> Conflict(string message) => new(default, ServiceError.Conflict, message);
    public static ServiceResult<T> Invalid(string message) => new(default, ServiceError.Validation, message);
}
