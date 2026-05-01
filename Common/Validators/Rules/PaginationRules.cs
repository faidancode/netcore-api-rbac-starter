using FluentValidation;

public static class PaginationRules
{
    public static IRuleBuilderOptions<T, int> ValidPage<T>(
        this IRuleBuilder<T, int> rule)
    {
        return rule.GreaterThan(0);
    }

    public static IRuleBuilderOptions<T, int> ValidLimit<T>(
        this IRuleBuilder<T, int> rule)
    {
        return rule.InclusiveBetween(1, 100);
    }
}