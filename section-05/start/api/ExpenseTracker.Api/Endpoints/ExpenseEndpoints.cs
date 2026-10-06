using System.Linq.Expressions;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Endpoints;

public static class ExpenseEndpoints
{
    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/expenses");

        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);

        return routes;
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var expense = await db.Expenses
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(AsResponse)
            .FirstOrDefaultAsync(cancellationToken);

        return expense is null ? Results.NotFound() : Results.Ok(expense);
    }

    private static async Task<IResult> CreateAsync(
        CreateExpenseRequest request,
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Amount <= 0)
        {
            errors["Amount"] = ["Amount must be greater than zero."];
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors["Description"] = ["Description is required."];
        }
        else if (request.Description.Length > 200)
        {
            errors["Description"] = ["Description must be 200 characters or fewer."];
        }

        if (request.Date == default)
        {
            errors["Date"] = ["Date is required."];
        }

        if (!Currencies.TryNormalize(request.Currency, out var currency))
        {
            errors["Currency"] = [$"Currency must be one of: {Currencies.AllowedList}."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var expense = new Expense
        {
            Amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            Currency = currency,
            Description = request.Description!.Trim(),
            Date = request.Date,
        };

        db.Expenses.Add(expense);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(expense).ReloadAsync(cancellationToken);

        var response = ToResponse(expense);
        return Results.Created($"/api/expenses/{response.Id}", response);
    }

    private static async Task<IResult> ListAsync(
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var expenses = await db.Expenses
            .AsNoTracking()
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .Select(AsResponse)
            .ToListAsync(cancellationToken);

        return Results.Ok(expenses);
    }

    private static readonly Expression<Func<Expense, ExpenseResponse>> AsResponse =
        e => new ExpenseResponse(e.Id, e.Amount, e.Currency, e.Description, e.Date, e.CreatedAt);

    private static readonly Func<Expense, ExpenseResponse> ToResponse = AsResponse.Compile();
}
