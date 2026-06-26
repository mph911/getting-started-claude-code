using ExpenseTracker.Api;
using ExpenseTracker.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace ExpenseTracker.Tests;

public sealed class PostgresApiFactory : WebApplicationFactory<IApiAssemblyMarker>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.3")
        .WithDatabase("expenses")
        .WithUsername("expenses")
        .WithPassword("expenses")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseDbContext>();
        await db.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ExpenseDbContext>>();

            services.AddDbContext<ExpenseDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }
}
