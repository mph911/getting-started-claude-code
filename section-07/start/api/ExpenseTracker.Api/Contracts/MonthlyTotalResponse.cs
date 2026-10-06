namespace ExpenseTracker.Api.Contracts;

/// <summary>Sum of one month's expenses in a single currency. <c>Month</c> is <c>YYYY-MM</c>.</summary>
public sealed record MonthlyTotalResponse(
    string Month,
    string Currency,
    decimal Total,
    int Count);
