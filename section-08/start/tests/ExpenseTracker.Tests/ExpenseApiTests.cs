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

    [Fact]
    public async Task Should_filter_inclusively_by_date_range_when_from_and_to_provided()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();

        var before = await CreateExpenseAsync(client, new(1m, "Range before window", new DateOnly(2025, 5, 1)), ct);
        var lowerBound = await CreateExpenseAsync(client, new(2m, "Range lower bound", new DateOnly(2025, 5, 10)), ct);
        var middle = await CreateExpenseAsync(client, new(3m, "Range middle", new DateOnly(2025, 5, 15)), ct);
        var upperBound = await CreateExpenseAsync(client, new(4m, "Range upper bound", new DateOnly(2025, 5, 20)), ct);
        var after = await CreateExpenseAsync(client, new(5m, "Range after window", new DateOnly(2025, 5, 31)), ct);

        var list = await client.GetFromJsonAsync<List<ExpenseResponse>>(
            "/api/expenses?from=2025-05-10&to=2025-05-20", JsonOptions, ct);

        Assert.NotNull(list);
        var ids = list.Select(e => e.Id).ToHashSet();
        Assert.Contains(lowerBound, ids);
        Assert.Contains(middle, ids);
        Assert.Contains(upperBound, ids);
        Assert.DoesNotContain(before, ids);
        Assert.DoesNotContain(after, ids);
    }

    [Fact]
    public async Task Should_filter_by_lower_bound_only_when_only_from_provided()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();

        var below = await CreateExpenseAsync(client, new(1m, "From-only below", new DateOnly(2025, 6, 1)), ct);
        var atOrAbove = await CreateExpenseAsync(client, new(2m, "From-only at bound", new DateOnly(2025, 6, 10)), ct);

        var list = await client.GetFromJsonAsync<List<ExpenseResponse>>(
            "/api/expenses?from=2025-06-10", JsonOptions, ct);

        Assert.NotNull(list);
        var ids = list.Select(e => e.Id).ToHashSet();
        Assert.Contains(atOrAbove, ids);
        Assert.DoesNotContain(below, ids);
    }

    [Fact]
    public async Task Should_return_400_problem_details_when_from_is_after_to()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/expenses?from=2025-12-31&to=2025-01-01", ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("from", out _));
    }

    [Fact]
    public async Task Should_return_400_problem_details_when_from_cannot_be_parsed()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/expenses?from=not-a-date", ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("from", out _));
    }

    [Fact]
    public async Task Should_return_400_problem_details_when_from_is_in_the_future()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1).ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/expenses?from={future}", ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("from", out _));
    }

    [Fact]
    public async Task Should_return_400_problem_details_when_to_is_in_the_future()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1).ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/expenses?to={future}", ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("to", out _));
    }

    [Fact]
    public async Task Should_return_only_expenses_from_the_last_7_days_when_listing_recent()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var today_ = await CreateExpenseAsync(client, new(1m, "Recent today", today), ct);
        var withinWindow = await CreateExpenseAsync(client, new(2m, "Recent mid-window", today.AddDays(-3)), ct);
        var lowerBound = await CreateExpenseAsync(client, new(3m, "Recent lower bound", today.AddDays(-7)), ct);
        var tooOld = await CreateExpenseAsync(client, new(4m, "Recent too old", today.AddDays(-8)), ct);
        var future = await CreateExpenseAsync(client, new(5m, "Recent future", today.AddDays(1)), ct);

        var list = await client.GetFromJsonAsync<List<ExpenseResponse>>(
            "/api/expenses/recent", JsonOptions, ct);

        Assert.NotNull(list);
        var ids = list.Select(e => e.Id).ToHashSet();
        Assert.Contains(today_, ids);
        Assert.Contains(withinWindow, ids);
        Assert.Contains(lowerBound, ids);
        Assert.DoesNotContain(tooOld, ids);
        Assert.DoesNotContain(future, ids);
    }

    [Theory]
    [InlineData(null, "EUR")]      // omitted entirely
    [InlineData("", "EUR")]        // empty string
    [InlineData("USD", "USD")]     // valid code
    [InlineData("usd", "USD")]     // lowercase normalized
    [InlineData("  usd  ", "USD")] // whitespace trimmed + normalized
    public async Task Should_resolve_currency_on_create(string? currency, string expected)
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();
        object payload = currency is null
            ? new { amount = 12.50m, description = "Currency theory", date = "2026-04-01" }
            : new { amount = 12.50m, description = "Currency theory", date = "2026-04-01", currency };

        var response = await client.PostAsJsonAsync("/api/expenses", payload, JsonOptions, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
        Assert.Equal(expected, created.GetProperty("currency").GetString());
    }

    [Fact]
    public async Task Should_return_400_problem_details_when_currency_is_not_allowed()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();
        var payload = new { amount = 12.50m, description = "Unsupported currency", date = "2026-04-04", currency = "JPY" };

        var response = await client.PostAsJsonAsync("/api/expenses", payload, JsonOptions, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Currency", out _));
    }

    [Fact]
    public async Task Should_group_totals_by_month_and_currency_ordered_most_recent_first()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();

        // Use a far-future year so this test's data doesn't collide with other tests sharing the DB.
        await CreateExpenseAsync(client, new(10.00m, "Totals June EUR a", new DateOnly(2099, 6, 5), "EUR"), ct);
        await CreateExpenseAsync(client, new(20.50m, "Totals June EUR b", new DateOnly(2099, 6, 20), "EUR"), ct);
        await CreateExpenseAsync(client, new(5.00m, "Totals June USD", new DateOnly(2099, 6, 10), "USD"), ct);
        await CreateExpenseAsync(client, new(7.25m, "Totals May EUR", new DateOnly(2099, 5, 15), "EUR"), ct);

        var totals = await client.GetFromJsonAsync<List<MonthlyCurrencyTotalResponse>>(
            "/api/expenses/totals", JsonOptions, ct);

        Assert.NotNull(totals);

        Assert.Equal(30.50m, Assert.Single(totals, t => t.Month == "2099-06" && t.Currency == "EUR").Total);
        Assert.Equal(5.00m, Assert.Single(totals, t => t.Month == "2099-06" && t.Currency == "USD").Total);
        Assert.Equal(7.25m, Assert.Single(totals, t => t.Month == "2099-05" && t.Currency == "EUR").Total);

        // Most recent month first: every 2099-06 row precedes the 2099-05 row.
        var seeded = totals.Where(t => t.Month is "2099-06" or "2099-05").ToList();
        var lastJuneIndex = seeded.FindLastIndex(t => t.Month == "2099-06");
        var firstMayIndex = seeded.FindIndex(t => t.Month == "2099-05");
        Assert.True(lastJuneIndex < firstMayIndex);
    }

    [Fact]
    public async Task Should_return_200_and_a_json_array_when_getting_totals()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();

        // The endpoint is unfiltered and the test DB is shared, so we can't force an
        // empty result; assert the contract instead: 200 OK with a well-formed JSON array.
        var response = await client.GetAsync("/api/expenses/totals", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var totals = await response.Content.ReadFromJsonAsync<List<MonthlyCurrencyTotalResponse>>(JsonOptions, ct);
        Assert.NotNull(totals);
    }

    private static async Task<Guid> CreateExpenseAsync(
        HttpClient client,
        CreateExpenseRequest request,
        CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync("/api/expenses", request, ct);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<ExpenseResponse>(JsonOptions, ct);
        Assert.NotNull(created);
        return created.Id;
    }
}
