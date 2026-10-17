using EmployeeManagement.Data;
using EmployeeManagement.Data.Data;
using EmployeeManagement.Data.Entities;
using EmployeeManagement.Data.Services;
using EmployeeManagement.Data.Test;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagament.Data.Test;

[TestClass]
public sealed class EmployeeManagementServiceTests : DatabaseTestBase
{
    private EmployeeManagementDbContext CreateVerificationContext()
    {
        var options = new DbContextOptionsBuilder<EmployeeManagementDbContext>()
            .UseSqlServer(Context.Database.GetConnectionString())
            .Options;

        return new EmployeeManagementDbContext(options);
    }

    private static EmployeeData ToData(Employee employee)
    {
        return new EmployeeData(
            employee.Id,
            employee.FirstName,
            employee.LastName,
            employee.Email,
            employee.HireDate,
            employee.DepartmentId);
    }

    [TestMethod]
    public async Task GetAllEmployees_ReturnsStoredValuesSortedWithoutTracking()
    {
        var expected = TestData.Employees
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .Select(ToData)
            .ToList();

        Context.ChangeTracker.Clear();

        var service = new EmployeeManagementService(Context);

        var employees = await service.GetAllEmployees();

        CollectionAssert.AreEqual(expected, employees);
        Assert.IsEmpty(Context.ChangeTracker.Entries<Employee>());
    }

    [TestMethod]
    public async Task GetAllEmployees_NoEmployees_ReturnsEmptyList()
    {
        await Context.Employees.ExecuteDeleteAsync();
        Context.ChangeTracker.Clear();

        var employees = await new EmployeeManagementService(Context).GetAllEmployees();

        Assert.IsEmpty(employees);
    }

    [TestMethod]
    public async Task GetDepartments_ReturnsSortedDepartmentsWithoutTracking()
    {
        var expectedNames = TestData.Departments
            .OrderBy(department => department.Name)
            .Select(department => department.Name)
            .ToList();

        Context.ChangeTracker.Clear();

        var departments = await new EmployeeManagementService(Context).GetDepartmentsAsync();

        CollectionAssert.AreEqual(expectedNames, departments.Select(department => department.Name).ToList());
        Assert.IsEmpty(Context.ChangeTracker.Entries<Department>());
    }

    [TestMethod]
    public async Task SaveAll_DeleteSelectedEmployees_PreservesUnselectedEmployee()
    {
        var service = new EmployeeManagementService(Context);
        var before = await service.GetAllEmployees();
        var deletedIds = before.Take(2).Select(employee => employee.Id).ToArray();

        await service.SaveAllAsync(new EmployeeChanges([], [], deletedIds));

        await using var verification = CreateVerificationContext();

        var remaining = await verification.Employees.SingleAsync();

        Assert.AreEqual(before[2], ToData(remaining));

    }

    [TestMethod]
    public async Task SaveAll_AddEmployees_ReturnsGeneratedIdsInRequestOrder()
    {
        var departmentId = TestData.Departments[0].Id;
        var first = new EmployeeData(
            0, "First", "Zulu", "first.new@example.com", new DateOnly(2025, 1, 10), departmentId);
        var second = new EmployeeData(
            0, "Second", "Alpha", null, new DateOnly(2025, 2, 20), departmentId);
        var service = new EmployeeManagementService(Context);

        var result = await service.SaveAllAsync(new EmployeeChanges([first, second], [], []));

        Assert.HasCount(2, result.NewEmployees);

        var savedFirst = result.NewEmployees[0];
        var savedSecond = result.NewEmployees[1];

        Assert.IsTrue(result.NewEmployees.All(employee => employee.Id > 0));
        Assert.AreNotEqual(savedFirst.Id, savedSecond.Id);

        var expected = new[]
        {
            first with { Id = savedFirst.Id },
            second with { Id = savedSecond.Id }
        };

        CollectionAssert.AreEqual(expected, result.NewEmployees.ToArray());

        await using var verification = CreateVerificationContext();

        var createdIds = result.NewEmployees.Select(employee => employee.Id).ToArray();
        var persisted = await verification.Employees
            .Where(employee => createdIds.Contains(employee.Id))
            .ToListAsync();

        CollectionAssert.AreEquivalent(expected, persisted.Select(ToData).ToArray());
    }

    [TestMethod]
    public async Task SaveAll_UpdateEmployee_PersistsAllEditableValues()
    {
        var service = new EmployeeManagementService(Context);
        var original = (await service.GetAllEmployees())[0];
        var modified = original with
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "updated@example.com",
            HireDate = new DateOnly(2023, 8, 15),
            DepartmentId = TestData.Departments[1].Id
        };

        var result = await service.SaveAllAsync(new EmployeeChanges([], [modified], []));

        var expected = modified;

        Assert.AreEqual(expected, result.ModifiedEmployees.Single());

        await using var verification = CreateVerificationContext();

        var persisted = await verification.Employees.SingleAsync(employee => employee.Id == original.Id);

        Assert.AreEqual(expected, ToData(persisted));
    }
    [TestMethod]
    public async Task SaveAll_NormalizesNamesAndEmail()
    {
        var service = new EmployeeManagementService(Context);
        var expected = (await service.GetAllEmployees())[0];
        var input = expected with
        {
            FirstName = $" {expected.FirstName} ",
            LastName = $" {expected.LastName} ",
            Email = $" {expected.Email} "
        };

        var result = await service.SaveAllAsync(new EmployeeChanges([], [input], []));

        Assert.AreEqual(expected, result.ModifiedEmployees.Single());

        await using var verification = CreateVerificationContext();

        var persisted = await verification.Employees.SingleAsync(employee => employee.Id == expected.Id);

        Assert.AreEqual(expected, ToData(persisted));
    }
}
