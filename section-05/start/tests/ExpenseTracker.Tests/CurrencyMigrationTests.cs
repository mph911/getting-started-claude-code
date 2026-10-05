using ExpenseTracker.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace ExpenseTracker.Tests;

public sealed class CurrencyMigrationTests(PostgresApiFactory factory)
{
    private const string MigrationBeforeCurrency = "20260506140534_InitialCreate";

    [Fact]
    public async Task Should_backfill_existing_expenses_to_EUR_when_applying_currency_migration()
    {
        var ct = TestContext.Current.CancellationToken;

        // Migrate a throwaway database in the same container so the shared one stays untouched.
        var connectionString = new NpgsqlConnectionStringBuilder(factory.ConnectionString)
        {
            Database = $"migration_{Guid.NewGuid():N}",
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<ExpenseDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var db = new ExpenseDbContext(options);
        try
        {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(MigrationBeforeCurrency, ct);

            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "Expenses" ("Id", "Amount", "Description", "Date")
                VALUES (gen_random_uuid(), 12.50, 'Legacy coffee', DATE '2026-01-15'),
                       (gen_random_uuid(), 3.20, 'Legacy bus', DATE '2026-01-16');
                """,
                ct);

            await migrator.MigrateAsync(cancellationToken: ct);

            var currencies = await db.Database
                .SqlQueryRaw<string>("""SELECT "Currency" AS "Value" FROM "Expenses" """)
                .ToListAsync(ct);

            Assert.Equal(2, currencies.Count);
            Assert.All(currencies, c => Assert.Equal("EUR", c));
        }
        finally
        {
            await db.Database.EnsureDeletedAsync(ct);
        }
    }
}
