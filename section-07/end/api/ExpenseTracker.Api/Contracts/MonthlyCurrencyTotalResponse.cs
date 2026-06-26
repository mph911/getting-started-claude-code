namespace ExpenseTracker.Api.Contracts;

public sealed record MonthlyCurrencyTotalResponse(string Month, string Currency, decimal Total);
