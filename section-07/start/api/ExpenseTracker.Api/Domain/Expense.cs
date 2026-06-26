namespace ExpenseTracker.Api.Domain;

public sealed class Expense
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required decimal Amount { get; init; }
    public required string Description { get; init; }
    public required DateOnly Date { get; init; }
    public required string Currency { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
