using System.Linq.Expressions;

namespace netcore_api_rbac_starter.Common.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, int page, int limit)
    {
        return query.Skip((page - 1) * limit).Take(limit);
    }

    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, string? sortParam)
    {
        if (string.IsNullOrWhiteSpace(sortParam))
            return query;

        var parts = sortParam.Split(':');
        if (parts.Length != 2) return query;

        var field = parts[0];
        var direction = parts[1].ToLower();

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = typeof(T).GetProperty(
            field,
            System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
        );

        if (property == null) return query;

        var propertyAccess = Expression.MakeMemberAccess(parameter, property);
        var orderByExpression = Expression.Lambda(propertyAccess, parameter);

        var methodName = direction == "desc" ? "OrderByDescending" : "OrderBy";

        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            [typeof(T), property.PropertyType],
            query.Expression,
            Expression.Quote(orderByExpression)
        );

        return query.Provider.CreateQuery<T>(resultExpression);
    }
}