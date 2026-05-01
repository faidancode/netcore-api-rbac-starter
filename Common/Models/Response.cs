namespace netcore_api_rbac_starter.Common.Models;

public class Response<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? Code { get; init; }
    public T? Data { get; init; }
    public PaginationMeta? Meta { get; init; }
    public object? Errors { get; init; }

    public static Response<T> Ok(T? data, string? message = null, PaginationMeta? meta = null)
        => new() { Success = true, Data = data, Message = message, Meta = meta };

    public static Response<T> OkMessage(string message)
        => new() { Success = true, Message = message };

    public static Response<T> Fail(string message, object? errors = null, string? code = null)
        => new()
        {
            Success = false,
            Message = message,
            Errors = errors,
            Code = code
        };
}

public class PaginationMeta
{
    public int Page { get; init; }
    public int Limit { get; init; }
    public int Total { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }

    public static PaginationMeta Create(int page, int limit, int total)
    {
        var safeLimit = limit < 1 ? 1 : limit;
        var safePage = page < 1 ? 1 : page;
        var totalPages = (int)Math.Ceiling((double)total / safeLimit);

        return new PaginationMeta
        {
            Page = safePage,
            Limit = safeLimit,
            Total = total,
            TotalPages = totalPages,
            HasNextPage = totalPages > 0 && safePage < totalPages,
            HasPreviousPage = safePage > 1 && totalPages > 0
        };
    }
}
