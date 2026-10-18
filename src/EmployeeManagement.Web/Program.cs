using EmployeeManagement.Data;
using EmployeeManagement.Shared.Editing;
using EmployeeManagement.Web.Components;
using NLog;
using NLog.Extensions.Logging;

var logger = LogManager.Setup()
    .LoadConfigurationFromFile(Path.Combine(AppContext.BaseDirectory, "NLog.config"))
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var connectionString = builder.Configuration.GetConnectionString("EmployeeManagement")
        ?? throw new InvalidOperationException("Configure ConnectionStrings:EmployeeManagement before starting.");

    builder.Logging.ClearProviders();
    builder.Logging.AddNLog();

    builder.Services.AddRazorComponents().AddInteractiveServerComponents();
    builder.Services.AddDataAccess(connectionString);
    builder.Services.AddTransient<EmployeeEditor>();

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
    }

    app.UseAntiforgery();
    app.MapStaticAssets();
    app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

    logger.Info("Web application starting.");

    app.Run();
}
catch (Exception exception)
{
    logger.Fatal(exception, "Web application stopped unexpectedly.");

    throw;
}
finally
{
    LogManager.Shutdown();
}
