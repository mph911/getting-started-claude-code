namespace ExpenseTracker.Api.Contracts;

public sealed record ExpenseResponse(
    Guid Id,
    decimal Amount,
    string Currency,
    string Description,
    DateOnly Date,
    DateTimeOffset CreatedAt);
