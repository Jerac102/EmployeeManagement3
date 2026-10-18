using System.ComponentModel;
using EmployeeManagement.Data.Data;
using EmployeeManagement.Data.Entities;
using EmployeeManagement.Data.Services;
using EmployeeManagement.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmployeeManagement.Shared.Editing;

public sealed class EmployeeEditor
{
    private readonly EmployeeManagementService _employeeService;
    private readonly ILogger<EmployeeEditor> _logger;

    public EmployeeEditor(
        EmployeeManagementService employeeService,
        ILogger<EmployeeEditor>? logger = null)
    {
        _employeeService = employeeService;
        _logger = logger ?? NullLogger<EmployeeEditor>.Instance;
    }

    public BindingList<EmployeeRow> Rows { get; } = new();

    public IReadOnlyList<Department> Departments { get; private set; } = [];

    public int ChangedRowCount => Rows.Count(row => row.HasChanges);

    public bool HasChanges => ChangedRowCount > 0;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Editor: loading employees...");
        var departments = await _employeeService.GetDepartmentsAsync(cancellationToken);

        _logger.LogInformation("Editor: loaded {DepartmentCount} departments.", departments.Count);
      
        var employees = await _employeeService.GetAllEmployeesAsync(cancellationToken);

        _logger.LogInformation("Editor: loaded {EmployeeCount} employees.", employees.Count);

        cancellationToken.ThrowIfCancellationRequested();

        var loadedRows = employees.Select(EmployeeRow.FromData).ToList();

        Departments = departments;

        // Keep the same list instance so the grid stays subscribed to it.
        Rows.RaiseListChangedEvents = false;

        try
        {
            Rows.Clear();

            foreach (var row in loadedRows)
            {
                Rows.Add(row);
            }
        }
        finally
        {
            Rows.RaiseListChangedEvents = true;
            Rows.ResetBindings();

            _logger.LogInformation("Editor: bound {RowCount} employee rows.", Rows.Count);
        }
    }

    public EmployeeRow AddEmployee()
    {
        var row = new EmployeeRow
        {
            DepartmentId = Departments.FirstOrDefault()?.Id ?? 0
        };

        Rows.Add(row);

        _logger.LogInformation("Editor: added a new unsaved employee row.");

        return row;
    }

    public void MarkForDelete(EmployeeRow row)
    {
        if (row.IsNew)
        {
            Rows.Remove(row);

            _logger.LogDebug("Editor: removed an employee row locally.");
        }
        else
        {
            row.IsMarkedForDelete = true;

            _logger.LogInformation("Editor: marked employee {EmployeeId} for deletion.", row.Id);
        }
    }

    public EmployeeChanges CreateChanges()
    {
        var newEmployees = Rows
            .Where(row => row.IsNew && !row.IsMarkedForDelete)
            .Select(row => row.ToData())
            .ToArray();

        var modifiedEmployees = Rows
            .Where(row => !row.IsNew && row.HasChanges && !row.IsMarkedForDelete)
            .Select(row => row.ToData())
            .ToArray();

        var deletedEmployeeIds = Rows
            .Where(row => !row.IsNew && row.IsMarkedForDelete)
            .Select(row => row.Id)
            .ToArray();

        return new EmployeeChanges(newEmployees, modifiedEmployees, deletedEmployeeIds);
    }

    public async Task SaveAllAsync(CancellationToken cancellationToken = default)
    {
        if (!HasChanges)
        {
            return;
        }

        // Remember row identities before accepting generated IDs and normalized values.
        var newRows = Rows.Where(row => row.IsNew && !row.IsMarkedForDelete).ToList();
        var modifiedRows = Rows.Where(row => !row.IsNew && row.HasChanges && !row.IsMarkedForDelete).ToList();
        var deletedRows = Rows.Where(row => row.IsMarkedForDelete).ToList();

        _logger.LogInformation("Editor: saving {ChangeCount} changes (before await).", ChangedRowCount);

        var result = await _employeeService.SaveAllAsync(CreateChanges(), cancellationToken);

        _logger.LogInformation("Editor: save completed (after await). Applying saved values to rows.");

        // Keep pending rows untouched until the complete batch has succeeded.
        for (var index = 0; index < newRows.Count; index++)
        {
            newRows[index].AcceptChanges(result.NewEmployees[index]);
        }

        for (var index = 0; index < modifiedRows.Count; index++)
        {
            modifiedRows[index].AcceptChanges(result.ModifiedEmployees[index]);
        }

        foreach (var row in deletedRows)
        {
            Rows.Remove(row);

            _logger.LogDebug("Editor: removed an employee row locally.");
        }
    }

    public void DiscardChanges()
    {
        _logger.LogInformation("Editor: discarding {ChangeCount} pending changes.", ChangedRowCount);

        foreach (var row in Rows.ToList())
        {
            if (row.IsNew)
            {
                Rows.Remove(row);

                _logger.LogDebug("Editor: removed an employee row locally.");
            }
            else
            {
                row.DiscardChanges();
            }
        }
    }
}