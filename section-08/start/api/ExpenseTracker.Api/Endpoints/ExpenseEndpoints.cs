using System.Globalization;
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
        group.MapGet("/totals", GetTotalsAsync);
        group.MapGet("/recent", ListRecentAsync);
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
            .Select(e => new ExpenseResponse(e.Id, e.Amount, e.Description, e.Date, e.Currency, e.CreatedAt))
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

        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? Currencies.Default
            : request.Currency.Trim().ToUpperInvariant();

        if (!Currencies.Allowed.Contains(currency))
        {
            errors["Currency"] = [$"Currency must be one of: {string.Join(", ", Currencies.Allowed)}."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var expense = new Expense
        {
            Amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            Description = request.Description!.Trim(),
            Date = request.Date,
            Currency = currency,
        };

        db.Expenses.Add(expense);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(expense).ReloadAsync(cancellationToken);

        var response = ToResponse(expense);
        return Results.Created($"/api/expenses/{response.Id}", response);
    }

    private static async Task<IResult> ListAsync(
        ExpenseDbContext db,
        CancellationToken cancellationToken,
        string? from = null,
        string? to = null)
    {
        var errors = new Dictionary<string, string[]>();

        var fromDate = ParseDateFilter(from, "from", errors);
        var toDate = ParseDateFilter(to, "to", errors);

        if (fromDate is { } f && toDate is { } t && f > t)
        {
            errors["from"] = ["from must be on or before to."];
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (fromDate is { } fromFuture && fromFuture > today)
        {
            errors["from"] = ["from cannot be in the future."];
        }

        if (toDate is { } toFuture && toFuture > today)
        {
            errors["to"] = ["to cannot be in the future."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var query = db.Expenses.AsNoTracking();

        if (fromDate is { } fromValue)
        {
            query = query.Where(e => e.Date >= fromValue);
        }

        if (toDate is { } toValue)
        {
            query = query.Where(e => e.Date <= toValue);
        }

        var expenses = await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => new ExpenseResponse(e.Id, e.Amount, e.Description, e.Date, e.Currency, e.CreatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(expenses);
    }

    private static async Task<IResult> GetTotalsAsync(
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var grouped = await db.Expenses
            .AsNoTracking()
            .GroupBy(e => new { e.Date.Year, e.Date.Month, e.Currency })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Currency,
                Total = g.Sum(e => e.Amount),
            })
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Month)
            .ThenBy(g => g.Currency)
            .ToListAsync(cancellationToken);

        var totals = grouped
            .Select(g => new MonthlyCurrencyTotalResponse(
                $"{g.Year:D4}-{g.Month:D2}", g.Currency, g.Total))
            .ToList();

        return Results.Ok(totals);
    }

    private static async Task<IResult> ListRecentAsync(
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var since = today.AddDays(-7);

        var query = db.Expenses.AsNoTracking();

        query = query.Where(e => e.Date >= since && e.Date <= today);

        var expenses = await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => new ExpenseResponse(e.Id, e.Amount, e.Description, e.Date, e.Currency, e.CreatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(expenses);
    }

    private static DateOnly? ParseDateFilter(string? value, string field, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        errors[field] = [$"{field} must be a valid date in YYYY-MM-DD format."];
        return null;
    }

    private static ExpenseResponse ToResponse(Expense e) =>
        new(e.Id, e.Amount, e.Description, e.Date, e.Currency, e.CreatedAt);
}
