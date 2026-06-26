using ExpenseTracker.Api.Endpoints;
using ExpenseTracker.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPersistence(builder.Configuration)
    .AddFrontendCors()
    .AddProblemDetails();

var app = builder.Build();

app.UseExpenseTrackerPipeline();
app.MapExpenseEndpoints();

app.Run();
