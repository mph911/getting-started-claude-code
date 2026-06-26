namespace ExpenseTracker.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseExpenseTrackerPipeline(this WebApplication app)
    {
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.UseCors(ServiceCollectionExtensions.FrontendCorsPolicy);
        }

        return app;
    }
}
