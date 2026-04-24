namespace netcore_api_rbac_starter.Common.Models;

public class PaginatedResponse<T>
{
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public PaginationMeta Meta { get; set; } = new();
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