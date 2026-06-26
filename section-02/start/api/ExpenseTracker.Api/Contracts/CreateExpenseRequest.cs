namespace ExpenseTracker.Api.Contracts;

public sealed record CreateExpenseRequest(decimal Amount, string? Description, DateOnly Date);
