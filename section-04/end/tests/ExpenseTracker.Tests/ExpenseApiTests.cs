using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExpenseTracker.Api.Contracts;
using Xunit;

namespace ExpenseTracker.Tests;

public sealed class ExpenseApiTests(PostgresApiFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Should_return_201_and_persist_when_posting_valid_expense()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();
        var payload = new CreateExpenseRequest(
            Amount: 12.50m,
            Description: "Coffee",
            Date: new DateOnly(2026, 1, 15));

        var response = await client.PostAsJsonAsync("/api/expenses", payload, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.StartsWith("/api/expenses/", response.Headers.Location?.OriginalString);

        var created = await response.Content.ReadFromJsonAsync<ExpenseResponse>(JsonOptions, ct);
        Assert.NotNull(created);
        Assert.Equal(payload.Amount, created.Amount);
        Assert.Equal(payload.Description, created.Description);
        Assert.Equal(payload.Date, created.Date);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.NotEqual(default, created.CreatedAt);

        var listed = await client.GetFromJsonAsync<List<ExpenseResponse>>("/api/expenses", JsonOptions, ct);
        Assert.NotNull(listed);
        Assert.Contains(listed, e => e.Id == created.Id);
    }

    [Fact]
    public async Task Should_return_expenses_ordered_by_date_descending_when_listing()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();
        var older = new CreateExpenseRequest(5m, "Older expense", new DateOnly(2026, 1, 1));
        var newer = new CreateExpenseRequest(7m, "Newer expense", new DateOnly(2026, 2, 1));

        await client.PostAsJsonAsync("/api/expenses", older, ct);
        await client.PostAsJsonAsync("/api/expenses", newer, ct);

        var list = await client.GetFromJsonAsync<List<ExpenseResponse>>("/api/expenses", JsonOptions, ct);

        Assert.NotNull(list);
        var dates = list.Select(e => e.Date).ToList();
        var sorted = dates.OrderByDescending(d => d).ToList();
        Assert.Equal(sorted, dates);
    }

    [Fact]
    public async Task Should_return_400_problem_details_when_description_is_empty()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();
        var payload = new CreateExpenseRequest(
            Amount: 9.99m,
            Description: "",
            Date: new DateOnly(2026, 3, 10));

        var response = await client.PostAsJsonAsync("/api/expenses", payload, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Description", out _));
    }
}
