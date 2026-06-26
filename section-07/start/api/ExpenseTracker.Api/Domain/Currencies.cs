namespace ExpenseTracker.Api.Domain;

public static class Currencies
{
    public const string Default = "EUR";

    public static readonly IReadOnlySet<string> Allowed =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "EUR", "USD", "GBP" };
}
