namespace ExpenseTracker.Api.Domain;

public static class Currencies
{
    public const string Default = "EUR";

    public static readonly string[] Allowed = ["EUR", "USD", "GBP"];

    public static readonly string AllowedList = string.Join(", ", Allowed);

    /// <summary>
    /// Resolves user input to a stored currency code: <c>null</c> means <see cref="Default"/>,
    /// anything else is trimmed, upper-cased, and must be in <see cref="Allowed"/>.
    /// </summary>
    public static bool TryNormalize(string? input, out string code)
    {
        code = input is null ? Default : input.Trim().ToUpperInvariant();
        return Allowed.Contains(code);
    }
}
