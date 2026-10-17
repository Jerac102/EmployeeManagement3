using System.ComponentModel;
using EmployeeManagement.Data.Data;
using EmployeeManagement.Data.Entities;
using EmployeeManagement.Data.Services;
using EmployeeManagement.Desktop.Models;

namespace EmployeeManagement.Desktop.Editing;

public sealed class EmployeeEditor
{
    private readonly EmployeeManagementService _employeeService;

    public EmployeeEditor(EmployeeManagementService employeeService)
    {
        _employeeService = employeeService;
    }

    public BindingList<EmployeeRow> Rows { get; } = new();

    public IReadOnlyList<Department> Departments { get; private set; } = [];

    public int ChangedRowCount => Rows.Count(row => row.HasChanges);

    public bool HasChanges => ChangedRowCount > 0;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _employeeService.GetDepartmentsAsync(cancellationToken);
        var employees = await _employeeService.GetAllEmployees(cancellationToken);

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
        }
    }

    public EmployeeRow AddEmployee()
    {
        var row = new EmployeeRow
        {
            DepartmentId = Departments.FirstOrDefault()?.Id ?? 0
        };

        Rows.Add(row);

        return row;
    }

    public void MarkForDelete(EmployeeRow row)
    {
        if (row.IsNew)
        {
            Rows.Remove(row);
        }
        else
        {
            row.IsMarkedForDelete = true;
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

        var result = await _employeeService.SaveAllAsync(CreateChanges(), cancellationToken);

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
        }
    }

    public void DiscardChanges()
    {
        foreach (var row in Rows.ToList())
        {
            if (row.IsNew)
            {
                Rows.Remove(row);
            }
            else
            {
                row.DiscardChanges();
            }
        }
    }
}