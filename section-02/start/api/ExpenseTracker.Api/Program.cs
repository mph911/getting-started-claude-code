using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ExpenseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddProblemDetails();

const string FrontendCorsPolicy = "frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseCors(FrontendCorsPolicy);
}

app.MapExpenseEndpoints();

app.Run();
