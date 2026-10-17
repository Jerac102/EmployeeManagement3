using EmployeeManagement.Data.Data;
using EmployeeManagement.Data.Entities;
using EmployeeManagement.Data.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmployeeManagement.Data.Services;

public class EmployeeManagementService
{
    private readonly EmployeeManagementDbContext employeeContext;
    private readonly ILogger<EmployeeManagementService> logger;

    public EmployeeManagementService(
        EmployeeManagementDbContext employeeContext,
        ILogger<EmployeeManagementService>? logger = null)
    {
        this.employeeContext = employeeContext;
        this.logger = logger ?? NullLogger<EmployeeManagementService>.Instance;
    }

    public async Task<List<EmployeeData>> GetAllEmployees(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Loading employees.");

        var employees = await employeeContext.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .Select(employee => new EmployeeData(
                employee.Id,
                employee.FirstName,
                employee.LastName,
                employee.Email,
                employee.HireDate,
                employee.DepartmentId))
            .ToListAsync(cancellationToken);

        logger.LogInformation("Loaded {EmployeeCount} employees.", employees.Count);

        return employees;
    }

    public async Task<List<Department>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        return await employeeContext.Departments
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeSaveResult> SaveAllAsync(
        EmployeeChanges changes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changes);
        cancellationToken.ThrowIfCancellationRequested();

        var validatedChanges = NormalizeAndValidateChanges(changes);

        if (employeeContext.ChangeTracker.HasChanges())
        {
            throw new InvalidOperationException(
                "The context already has pending changes. Save All requires a clean context.");
        }

        try
        {
            logger.LogInformation(
                "Saving employee changes: {NewCount} new, {ModifiedCount} modified, {DeletedCount} deleted.",
                validatedChanges.NewEmployees.Count,
                validatedChanges.ModifiedEmployees.Count,
                validatedChanges.DeletedEmployeeIds.Count);

            var (added, updated) = ApplyChanges(validatedChanges);

            // SQL Server saves all operations in one transaction.
            await employeeContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Employee changes saved successfully.");

            // Preserve request order so the editor can match generated IDs to new rows.
            return new EmployeeSaveResult(
                added.Select(ToData).ToArray(),
                updated.Select(ToData).ToArray());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DetachFailedChanges();

            logger.LogInformation("Saving employee changes was cancelled.");

            throw;
        }
        catch (Exception exception)
        {
            DetachFailedChanges();

            logger.LogError(exception, "Saving employee changes failed.");

            throw;
        }

    }

    private static EmployeeChanges NormalizeAndValidateChanges(EmployeeChanges changes)
    {
        var newEmployees = changes.NewEmployees
            .Select(EmployeeValidator.NormalizeAndValidate)
            .ToArray();

        var modifiedEmployees = changes.ModifiedEmployees
            .Select(EmployeeValidator.NormalizeAndValidate)
            .ToArray();

        var deletedIds = changes.DeletedEmployeeIds.ToArray();

        if (newEmployees.Any(employee => employee.Id != 0))
        {
            throw new ArgumentException("New employees must not have a database ID.", nameof(changes));
        }

        var existingIds = modifiedEmployees
            .Select(employee => employee.Id)
            .Concat(deletedIds)
            .ToArray();

        if (existingIds.Any(id => id <= 0)
            || existingIds.Distinct().Count() != existingIds.Length)
        {
            throw new ArgumentException(
                "Existing employee IDs must be positive and occur in only one operation.",
                nameof(changes));
        }

        return new EmployeeChanges(newEmployees, modifiedEmployees, deletedIds);
    }

    private (List<Employee> Added, List<Employee> Updated) ApplyChanges(EmployeeChanges changes)
    {
        var added = new List<Employee>();
        var updated = new List<Employee>();

        foreach (var data in changes.NewEmployees)
        {
            var employee = new Employee();

            ApplyValues(employee, data);

            employeeContext.Employees.Add(employee);
            added.Add(employee);
        }

        foreach (var data in changes.ModifiedEmployees)
        {
            var employee = GetOrAttachEmployee(data.Id);
            ApplyValues(employee, data);

            // No database read is needed. Only the submitted editable fields are updated.
            var entry = employeeContext.Entry(employee);

            entry.Property(e => e.FirstName).IsModified = true;
            entry.Property(e => e.LastName).IsModified = true;
            entry.Property(e => e.Email).IsModified = true;
            entry.Property(e => e.HireDate).IsModified = true;
            entry.Property(e => e.DepartmentId).IsModified = true;

            updated.Add(employee);
        }

        foreach (var id in changes.DeletedEmployeeIds)
        {
            var employee = GetOrAttachEmployee(id);
            employeeContext.Employees.Remove(employee);
        }

        return (added, updated);
    }

    private Employee GetOrAttachEmployee(int id)
    {
        // Local searches only tracked objects. It does not query the database.
        var employee = employeeContext.Employees.Local.FirstOrDefault(employee => employee.Id == id);

        if (employee is not null)
        {
            return employee;
        }

        employee = new Employee { Id = id };

        employeeContext.Employees.Attach(employee);

        return employee;
    }

    private void DetachFailedChanges()
    {
        // The clean-context guard ensures these pending employee writes belong to this batch.
        var failedEntries = employeeContext.ChangeTracker.Entries<Employee>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in failedEntries)
        {
            entry.State = EntityState.Detached;
        }
    }

    private static void ApplyValues(Employee employee, EmployeeData data)
    {
        employee.FirstName = data.FirstName;
        employee.LastName = data.LastName;
        employee.Email = data.Email;
        employee.HireDate = data.HireDate;
        employee.DepartmentId = data.DepartmentId;
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
}