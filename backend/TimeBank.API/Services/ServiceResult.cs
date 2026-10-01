namespace TimeBank.API.Services;

// Tipos de error que puede devolver un servicio; el controlador los traduce a códigos HTTP
public enum ServiceErrorType
{
    None,
    Validation,   // 400 Bad Request
    Unauthorized, // 401 Unauthorized
    Forbidden,    // 403 Forbidden
    NotFound,     // 404 Not Found
    Conflict      // 409 Conflict
}

// Resultado de una operación del servicio: o trae el dato (Value) o trae el error.
// Así el servicio no necesita saber nada de HTTP y el controlador queda corto.
public class ServiceResult<T>
{
    public bool Success { get; private init; }
    public T? Value { get; private init; }
    public string? Error { get; private init; }
    public ServiceErrorType ErrorType { get; private init; }

    public static ServiceResult<T> Ok(T value) =>
        new() { Success = true, Value = value, ErrorType = ServiceErrorType.None };

    public static ServiceResult<T> Fail(ServiceErrorType type, string error) =>
        new() { Success = false, Error = error, ErrorType = type };
}
