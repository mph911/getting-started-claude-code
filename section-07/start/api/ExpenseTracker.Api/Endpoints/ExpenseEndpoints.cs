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
        group.MapGet("/recent", ListRecentAsync);
        group.MapGet("/monthly-totals", ListMonthlyTotalsAsync);
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

        var amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        var description = request.Description?.Trim();

        if (amount <= 0)
        {
            errors["Amount"] = ["Amount must be greater than zero."];
        }

        if (string.IsNullOrEmpty(description))
        {
            errors["Description"] = ["Description is required."];
        }
        else if (description.Length > 200)
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
            Amount = amount,
            Description = description!,
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

    // `today` is the caller's local calendar date; the server's UTC date can lag or lead it.
    private static async Task<IResult> ListRecentAsync(
        ExpenseDbContext db,
        CancellationToken cancellationToken,
        string? today = null)
    {
        var errors = new Dictionary<string, string[]>();
        var todayDate = ParseDateFilter(today, "today", errors);

        // The window reaches back 7 days; anything earlier would underflow DateOnly.MinValue.
        if (todayDate is { } d && d < DateOnly.MinValue.AddDays(7))
        {
            errors["today"] = ["today is out of range."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var until = todayDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var since = until.AddDays(-7);

        var query = db.Expenses.AsNoTracking();

        query = query.Where(e => e.Date >= since && e.Date <= until);

        var expenses = await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => new ExpenseResponse(e.Id, e.Amount, e.Description, e.Date, e.Currency, e.CreatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(expenses);
    }

    // Grouped by spent-on month and Currency: amounts in different currencies are never summed.
    private static async Task<IResult> ListMonthlyTotalsAsync(
        ExpenseDbContext db,
        CancellationToken cancellationToken)
    {
        var totals = await db.Expenses
            .AsNoTracking()
            .GroupBy(e => new { e.Date.Year, e.Date.Month, e.Currency })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Currency,
                Total = g.Sum(e => e.Amount),
                Count = g.Count(),
            })
            .OrderByDescending(t => t.Year)
            .ThenByDescending(t => t.Month)
            .ThenBy(t => t.Currency)
            .ToListAsync(cancellationToken);

        var response = totals
            .Select(t => new MonthlyTotalResponse(
                $"{t.Year:D4}-{t.Month:D2}",
                t.Currency,
                t.Total,
                t.Count))
            .ToList();

        return Results.Ok(response);
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
