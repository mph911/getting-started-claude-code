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

    [Theory]
    [InlineData("from")]
    [InlineData("to")]
    public async Task Should_return_400_problem_details_when_date_filter_is_in_the_future(string field)
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2).ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/expenses?{field}={future}", ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty(field, out _));
    }

    [Fact]
    public async Task Should_accept_today_as_date_filter()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/expenses?from={today}&to={today}", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
