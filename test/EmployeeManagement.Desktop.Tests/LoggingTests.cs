using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Targets;

namespace EmployeeManagement.Desktop.Tests;

[TestClass]
public class LoggingTests
{
    [TestMethod]
    public void Configuration_WritesApplicationEventsThroughMicrosoftLogger()
    {
        var path = Path.Combine(Path.GetTempPath(), $"EmployeeManagement_LogTest_{Guid.NewGuid():N}.log");

        try
        {
            using var logFactory = new LogFactory();

            logFactory.ThrowConfigExceptions = true;
            logFactory.Setup().LoadConfigurationFromFile(Path.Combine(AppContext.BaseDirectory, "NLog.config"));

            var target = logFactory.Configuration!.FindTargetByName<FileTarget>("file");

            Assert.IsNotNull(target);

            target.FileName = path;
            logFactory.ReconfigExistingLoggers();

            using var loggerFactory = LoggerFactory.Create(logging => logging.AddNLog(_ => logFactory));

            var logger = loggerFactory.CreateLogger("EmployeeManagement.LoggingTest");

            logger.LogInformation("Loaded {EmployeeCount} employees.", 3);
            logger.LogError(new InvalidOperationException("private@example.com"), "Save failed.");

            logFactory.Flush();
            logFactory.Shutdown();

            var contents = File.ReadAllText(path);

            StringAssert.Contains(contents, "Loaded 3 employees.");
            StringAssert.Contains(contents, "Save failed.");
            StringAssert.Contains(contents, nameof(InvalidOperationException));
            Assert.AreEqual(-1, contents.IndexOf("private@example.com", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(path);
        }
    }
}