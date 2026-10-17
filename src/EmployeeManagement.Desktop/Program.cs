using System.Text.Json;
using EmployeeManagement.Data;
using EmployeeManagement.Desktop.Forms;
using EmployeeManagement.Desktop.Editing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace EmployeeManagement.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var logger = LogManager.GetCurrentClassLogger();

        try
        {
            var logConfigPath = Path.Combine(AppContext.BaseDirectory, "NLog.config");

            LogManager.Setup().LoadConfigurationFromFile(logConfigPath);

            ApplicationConfiguration.Initialize();

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            RegisterGlobalExceptionHandling(logger);

            logger.Info("Application starting.");

            var connectionString = GetConnectionStringOrThrow();
            using var provider = RegisterServices(connectionString);
            using var scope = provider.CreateScope();

            Application.Run(scope.ServiceProvider.GetRequiredService<EmployeeListForm>());
        }
        catch (Exception exception)
        {
            logger.Fatal(exception, "Application could not start or continue.");

            MessageBox.Show(
                exception.Message,
                "Application error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            logger.Info("Application stopping.");

            LogManager.Shutdown();
        }
    }

    private static ServiceProvider RegisterServices(string connectionString)
    {
        var services = new ServiceCollection();

        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            logging.AddNLog();
        });

        services.AddDataAccess(connectionString);
        services.AddTransient<EmployeeEditor>();
        services.AddTransient<EmployeeListForm>();
        return services.BuildServiceProvider();
    }

    private static string GetConnectionStringOrThrow()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        using var settings = JsonDocument.Parse(File.ReadAllText(settingsPath));
        var connectionString = settings.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("EmployeeManagement")
            .GetString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The database connection string is missing.");
        }

        return connectionString;
    }

    private static void RegisterGlobalExceptionHandling(Logger logger)
    {
        Application.ThreadException += (_, e) =>
        {
            logger.Error(e.Exception, "Unhandled UI exception.");

            MessageBox.Show(
                "An unexpected error occurred. See the application log for details.",
                "Unexpected error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            logger.Fatal(e.ExceptionObject as Exception, "Unhandled application exception.");
    }
}