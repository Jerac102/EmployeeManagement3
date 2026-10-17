using System.Text.RegularExpressions;
using EmployeeManagament.Data.Test.Data;
using EmployeeManagement.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Data.Test;

public abstract class DatabaseTestBase
{
    private const string ServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true;";

    private const string ScriptDatabaseName = "EmployeeManagement";

    private string _databaseName = null!;

    protected EmployeeManagementDbContext Context { get; private set; } = null!;
    private protected TestData TestData { get; private set; } = null!;

    [TestInitialize]
    public void SetUp()
    {
        _databaseName = $"EmployeeManagement_Test_{Guid.NewGuid():N}";
        ExecuteScript(_databaseName);

        var options = new DbContextOptionsBuilder<EmployeeManagementDbContext>()
            .UseSqlServer($"{ServerConnectionString}Database={_databaseName};")
            .Options;

        Context = new EmployeeManagementDbContext(options);

        TestData = new TestData();
        InsertTestDataIntoDatabase();
    }

    private void InsertTestDataIntoDatabase()
    {
        Context.Departments.AddRange(TestData.Departments);
        Context.SaveChanges();
        var employees = TestData.Employees;
        var departments = Context.Departments.ToList();
        employees.ForEach(x => x.DepartmentId = departments.First().Id);
        Context.Employees.AddRange(employees);

        Context.SaveChanges();
    }   

    [TestCleanup]
    public void TearDown()
    {
        Context.Dispose();
        SqlConnection.ClearAllPools();

        using var connection = new SqlConnection(ServerConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}];";
        command.ExecuteNonQuery();
    }

    private static void ExecuteScript(string databaseName)
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "sql", "CreateDatabase.sql");
        var script = File.ReadAllText(scriptPath).Replace(ScriptDatabaseName, databaseName);
        var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        using var connection = new SqlConnection(ServerConnectionString);
        connection.Open();

        foreach (var batch in batches.Where(b => !string.IsNullOrWhiteSpace(b)))
        {
            using var command = connection.CreateCommand();
            command.CommandText = batch;
            command.ExecuteNonQuery();
        }
    }

    protected static Employee CreateEmployee(Department department, string lastName = "Sample")
        => new()
        {
            FirstName = "Max",
            LastName = lastName,
            Email = $"{lastName.ToLowerInvariant()}@example.com",
            HireDate = new DateOnly(2024, 1, 15),
            DepartmentId = department.Id
        };
}
