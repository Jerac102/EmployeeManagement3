using EmployeeManagement.Data;
using EmployeeManagement.Data.Data;
using EmployeeManagement.Data.Services;
using EmployeeManagement.Data.Test;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagament.Data.Test;

[TestClass]
public class EmployeeSaveAllTests : DatabaseTestBase
{
    [TestMethod]
    public async Task SaveAll_Success_KeepsTrackedEntitiesAndReusesThemOnNextSave()
    {
        var service = new EmployeeManagementService(Context);
        var employee = TestData.Employees[0];
        var before = (await service.GetAllEmployeesAsync()).Single(data => data.Id == employee.Id);

        var firstChange = before with { FirstName = "First change" };
        var secondChange = before with { FirstName = "Second change" };

        var firstResult = await service.SaveAllAsync(new EmployeeChanges(
            [CreateEmployeeData()], [firstChange], []));

        Assert.AreSame(employee, Context.Employees.Local.Single(entity => entity.Id == employee.Id));
        Assert.AreEqual(EntityState.Unchanged, Context.Entry(employee).State);

        var createdData = firstResult.NewEmployees.Single();
        var createdEntity = Context.Employees.Local.Single(entity => entity.Id == createdData.Id);

        Assert.AreEqual(EntityState.Unchanged, Context.Entry(createdEntity).State);

        await service.SaveAllAsync(new EmployeeChanges([], [secondChange], []));

        Assert.AreSame(employee, Context.Employees.Local.Single(entity => entity.Id == employee.Id));
        Assert.AreEqual(secondChange.FirstName, employee.FirstName);
        Assert.AreEqual(EntityState.Unchanged, Context.Entry(employee).State);

        await service.SaveAllAsync(new EmployeeChanges([], [], [createdData.Id]));

        Assert.AreEqual(EntityState.Detached, Context.Entry(createdEntity).State);
        Assert.AreEqual(EntityState.Unchanged, Context.Entry(employee).State);

        await using var verification = CreateVerificationContext();

        Assert.AreEqual(secondChange.FirstName, (await verification.Employees.SingleAsync(e => e.Id == employee.Id)).FirstName);
        Assert.IsFalse(await verification.Employees.AnyAsync(e => e.Id == createdData.Id));
    }

    [TestMethod]
    public async Task SaveAll_Failure_DetachesFailedWritesButPreservesUnrelatedTracking()
    {
        var service = new EmployeeManagementService(Context);
        var before = await service.GetAllEmployeesAsync();
        var modified = TestData.Employees.Single(employee => employee.Id == before[0].Id);
        var untouched = TestData.Employees.Single(employee => employee.Id == before[2].Id);
        var department = TestData.Departments[0];

        var invalidEmployee = CreateEmployeeData() with { DepartmentId = int.MaxValue };
        var failedChange = before[0] with { FirstName = "Failed change" };
        var retry = before[0] with { FirstName = "Retry" };

        await Assert.ThrowsAsync<DbUpdateException>(() => service.SaveAllAsync(new EmployeeChanges(
            [invalidEmployee], [failedChange], [before[1].Id])));

        Assert.AreEqual(EntityState.Detached, Context.Entry(modified).State);
        Assert.AreEqual(EntityState.Unchanged, Context.Entry(untouched).State);
        Assert.AreEqual(EntityState.Unchanged, Context.Entry(department).State);
        Assert.IsFalse(Context.ChangeTracker.HasChanges());

        await service.SaveAllAsync(new EmployeeChanges([], [retry], []));

        await using var verification = CreateVerificationContext();

        Assert.AreEqual(retry.FirstName, (await verification.Employees.SingleAsync(e => e.Id == modified.Id)).FirstName);
        Assert.AreEqual(TestData.Employees.Count, await verification.Employees.CountAsync());
    }

    private EmployeeManagementDbContext CreateVerificationContext()
    {
        var options = new DbContextOptionsBuilder<EmployeeManagementDbContext>()
            .UseSqlServer(Context.Database.GetConnectionString())
            .Options;

        return new EmployeeManagementDbContext(options);
    }

    private EmployeeData CreateEmployeeData()
    {
        return new EmployeeData(
            Id: 0,
            FirstName: "New",
            LastName: "Employee",
            Email: "new@example.com",
            HireDate: new DateOnly(2024, 1, 15),
            DepartmentId: TestData.Departments[0].Id);
    }

    [TestMethod]
    public async Task SaveAll_InsertsUpdatesAndDeletesTogether()
    {
        var service = new EmployeeManagementService(Context);
        var before = await service.GetAllEmployeesAsync();
        var modified = before[0] with { FirstName = "Updated" };
        var newEmployee = CreateEmployeeData();

        var result = await service.SaveAllAsync(new EmployeeChanges(
            [newEmployee], [modified], [before[1].Id]));

        await using var verification = CreateVerificationContext();

        var persisted = await new EmployeeManagementService(verification).GetAllEmployeesAsync();
        var created = result.NewEmployees.Single();
        var expectedCreated = newEmployee with { Id = created.Id };

        Assert.IsGreaterThan(0, created.Id);
        Assert.AreEqual(expectedCreated, created);
        Assert.AreEqual(modified, result.ModifiedEmployees.Single());
        CollectionAssert.AreEquivalent(
            new[] { expectedCreated, modified, before[2] },
            persisted);
    }

    [TestMethod]
    public async Task SaveAll_DatabaseFailure_RollsBackAndAllowsRetry()
    {
        var service = new EmployeeManagementService(Context);
        var before = await service.GetAllEmployeesAsync();

        // An invalid foreign key forces SQL Server to reject the batch.
        var invalid = CreateEmployeeData() with { DepartmentId = int.MaxValue };
        var modified = before[0] with { FirstName = "Changed" };

        await Assert.ThrowsAsync<DbUpdateException>(() => service.SaveAllAsync(
            new EmployeeChanges([invalid], [modified], [before[1].Id])));

        await using var verification = CreateVerificationContext();

        var persisted = await new EmployeeManagementService(verification).GetAllEmployeesAsync();

        CollectionAssert.AreEquivalent(before, persisted);

        var employeeChanges = new EmployeeChanges([CreateEmployeeData()], [], []);
        var result = await service.SaveAllAsync(employeeChanges);

        Assert.HasCount(1, result.NewEmployees);
        Assert.AreEqual(before.Count + employeeChanges.NewEmployees.Count, await verification.Employees.CountAsync());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SaveAll_MissingEmployee_DoesNotApplyOtherChanges(bool update)
    {
        var service = new EmployeeManagementService(Context);
        var before = await service.GetAllEmployeesAsync();

        var newEmployee = CreateEmployeeData();
        var missingEmployee = before[0] with { Id = int.MaxValue };

        var changes = update
            ? new EmployeeChanges([newEmployee], [missingEmployee], [])
            : new EmployeeChanges([newEmployee], [], [missingEmployee.Id]);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => service.SaveAllAsync(changes));

        await using var verification = CreateVerificationContext();

        Assert.AreEqual(before.Count, await verification.Employees.CountAsync());
    }

    [TestMethod]
    public async Task SaveAll_ConflictingOperations_AreRejected()
    {
        var service = new EmployeeManagementService(Context);
        var employee = (await service.GetAllEmployeesAsync())[0];

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAllAsync(
            new EmployeeChanges([], [employee], [employee.Id])));

    }

    [TestMethod]
    public async Task SaveAll_InvalidName_IsRejectedBeforeSaving()
    {
        var service = new EmployeeManagementService(Context);

        var invalidEmployee = CreateEmployeeData() with { FirstName = " " };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAllAsync(
            new EmployeeChanges([invalidEmployee], [], [])));

    }

    [TestMethod]
    public async Task SaveAll_EmptyChanges_ReturnsEmptyResult()
    {
        var result = await new EmployeeManagementService(Context)
            .SaveAllAsync(new EmployeeChanges([], [], []));

        Assert.IsEmpty(result.NewEmployees);
        Assert.IsEmpty(result.ModifiedEmployees);
    }

    [TestMethod]
    public async Task SaveAll_CancelledRequest_DoesNotSave()
    {
        using var cancellation = new CancellationTokenSource();

        cancellation.Cancel();

        var service = new EmployeeManagementService(Context);

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.SaveAllAsync(
            new EmployeeChanges([CreateEmployeeData()], [], []), cancellation.Token));

    }
}