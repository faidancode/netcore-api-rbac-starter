namespace netcore_api_rbac_starter.Common.Models;

public class Response<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public PaginationMeta? Meta { get; set; }

    public static Response<T> Ok(T? data, string? message = null, PaginationMeta? meta = null)
        => new() { Success = true, Data = data, Message = message, Meta = meta };

    public static Response<T> Fail(string message)
        => new() { Success = false, Message = message };
}

public class PaginationMeta
{
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }

    public static PaginationMeta Create(int page, int limit, int total)
    {
        var totalPages = (int)Math.Ceiling((double)total / limit);
        return new PaginationMeta
        {
            Page = page,
            Limit = limit,
            Total = total,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }
}
