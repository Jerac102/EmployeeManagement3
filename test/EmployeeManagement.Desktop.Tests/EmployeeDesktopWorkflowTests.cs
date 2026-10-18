using EmployeeManagement.Data;
using EmployeeManagement.Data.Entities;
using EmployeeManagement.Data.Services;
using EmployeeManagement.Shared.Editing;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Desktop.Tests;

[TestClass]
public class EmployeeDesktopWorkflowTests
{
    private EmployeeManagementDbContext _context = null!;
    private EmployeeEditor _editor = null!;

    [TestInitialize]
    public async Task SetUp()
    {
        var database = $"EmployeeManagement_DesktopTest_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<EmployeeManagementDbContext>()
            .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true;")
            .Options;

        _context = new EmployeeManagementDbContext(options);

        await _context.Database.EnsureCreatedAsync();

        var department = new Department { Name = "IT" };

        _context.Departments.Add(department);
        _context.Employees.AddRange(
            new Employee
            {
                FirstName = "First", LastName = "Employee", Department = department,
                HireDate = new DateOnly(2024, 1, 15)
            },
            new Employee
            {
                FirstName = "Second", LastName = "Employee", Department = department,
                HireDate = new DateOnly(2024, 1, 15)
            });

        await _context.SaveChangesAsync();

        _editor = new EmployeeEditor(new EmployeeManagementService(_context));

        await _editor.LoadAsync();
    }

    [TestCleanup]
    public async Task TearDown()
    {
        if (_context is not null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task SaveAll_AcceptsGeneratedIdsAndChangesOnlyAfterDatabaseSuccess()
    {
        var modified = _editor.Rows[0];
        var deleted = _editor.Rows[1];
        var added = _editor.AddEmployee();

        modified.FirstName = "Changed";
        added.FirstName = " New ";
        added.LastName = " Employee ";
        added.DepartmentId = _editor.Departments[0].Id;

        _editor.MarkForDelete(deleted);

        Assert.AreEqual(2, await _context.Employees.CountAsync());

        await _editor.SaveAllAsync();

        Assert.IsFalse(_editor.HasChanges);
        Assert.IsFalse(added.IsNew);
        Assert.AreEqual("New", added.FirstName);
        Assert.AreEqual("Employee", added.LastName);
        CollectionAssert.DoesNotContain(_editor.Rows, deleted);
        Assert.AreEqual(2, await _context.Employees.CountAsync());

        var persisted = await new EmployeeManagementService(_context).GetAllEmployeesAsync();

        CollectionAssert.AreEquivalent(persisted, _editor.Rows.Select(row => row.ToData()).ToList());

        _editor.DiscardChanges();

        Assert.AreEqual("Changed", modified.FirstName);
        CollectionAssert.Contains(_editor.Rows, added);
    }

    [TestMethod]
    public async Task SaveAll_FailedBatch_PreservesUnsavedRowsForCorrectionAndRetry()
    {
        var deleted = _editor.Rows[0];
        var modified = _editor.Rows[1];
        var added = _editor.AddEmployee();

        modified.LastName = "Changed";
        added.FirstName = "New";
        added.LastName = "Employee";
        added.DepartmentId = int.MaxValue;

        _editor.MarkForDelete(deleted);

        await Assert.ThrowsAsync<DbUpdateException>(() => _editor.SaveAllAsync());

        Assert.IsTrue(added.IsNew);
        Assert.IsTrue(modified.HasChanges);
        Assert.IsTrue(deleted.IsMarkedForDelete);
        CollectionAssert.Contains(_editor.Rows, deleted);
        Assert.AreEqual(2, await _context.Employees.CountAsync());

        added.DepartmentId = _editor.Departments[0].Id;

        await _editor.SaveAllAsync();

        Assert.IsFalse(_editor.HasChanges);
        Assert.IsFalse(added.IsNew);
        CollectionAssert.DoesNotContain(_editor.Rows, deleted);
    }

    [TestMethod]
    public async Task Discard_DoesNotWriteStagedChangesToDatabase()
    {
        _editor.Rows[0].FirstName = "Changed";
        _editor.MarkForDelete(_editor.Rows[1]);
        _editor.AddEmployee();

        _editor.DiscardChanges();

        var persisted = await new EmployeeManagementService(_context).GetAllEmployeesAsync();

        Assert.IsFalse(_editor.HasChanges);
        Assert.HasCount(2, _editor.Rows);
        CollectionAssert.AreEquivalent(persisted, _editor.Rows.Select(row => row.ToData()).ToList());
    }
}
