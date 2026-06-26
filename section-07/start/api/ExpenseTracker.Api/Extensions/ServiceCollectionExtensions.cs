using ExpenseTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Extensions;

public static class ServiceCollectionExtensions
{
    internal const string FrontendCorsPolicy = "frontend";

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration) =>
        services.AddDbContext<ExpenseDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

    public static IServiceCollection AddFrontendCors(this IServiceCollection services) =>
        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy => policy
                .WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod());
        });
}
