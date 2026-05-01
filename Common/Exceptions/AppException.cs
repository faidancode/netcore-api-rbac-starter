namespace netcore_api_rbac_starter.Common.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }

    public AppException(string message, int statusCode = 400, string? code = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code ?? "APP_ERROR";
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string resource, object id)
        : base($"{resource} with id '{id}' was not found.", 404, $"{resource.ToUpper()}_NOT_FOUND") { }

    public NotFoundException(string message)
        : base(message, 404, "NOT_FOUND") { }
}

public class ConflictException : AppException
{
    public ConflictException(string message, string code = "CONFLICT")
        : base(message, 409, code) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Forbidden")
        : base(message, 403, "FORBIDDEN") { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized")
        : base(message, 401, "UNAUTHORIZED") { }
}