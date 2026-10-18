using EmployeeManagement.Shared.Editing;
using EmployeeManagement.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace EmployeeManagement.Web.Components.Pages;

public partial class Employees : OwningComponentBase<EmployeeEditor>
{
    private EditContext _editContext = null!;
    private bool _busy;
    private bool _loaded;
    private string _search = string.Empty;
    private string? _message;
    private string? _error;

    private EmployeeEditor Editor => Service;

    private bool CanEdit => _loaded && !_busy;

    private IEnumerable<EmployeeRow> VisibleRows => Editor.Rows.Where(row =>
        string.IsNullOrWhiteSpace(_search)
        || $"{row.FirstName} {row.LastName} {row.Email}".Contains(_search, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        ResetForm();

        await LoadAsync();
    }

    private void ResetForm()
    {
        _editContext = new EditContext(Editor);
    }

    private async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            await Editor.LoadAsync();

            _loaded = true;
            ResetForm();
        });
    }

    private void AddEmployee()
    {
        if (!CanEdit || Editor.Departments.Count == 0)
        {
            return;
        }

        Editor.AddEmployee();
        _search = string.Empty;
        _message = null;
    }

    private async Task SaveAllAsync()
    {
        if (!CanEdit || !_editContext.Validate())
        {
            return;
        }

        await RunAsync(async () =>
        {
            await Editor.SaveAllAsync();

            ResetForm();
            _message = "All changes saved. Your desktop app will see them when it refreshes.";
        });
    }

    private async Task DeleteAsync(EmployeeRow row)
    {
        if (!CanEdit)
        {
            return;
        }

        if (row.IsMarkedForDelete)
        {
            row.IsMarkedForDelete = false;
        }
        else if (await JS.InvokeAsync<bool>("confirm", $"Mark {row.FirstName} {row.LastName} for deletion? Save All will apply it to the database."))
        {
            Editor.MarkForDelete(row);
        }

        ResetForm();
        _message = null;
    }

    private async Task DiscardAsync()
    {
        if (CanEdit && await JS.InvokeAsync<bool>("confirm", "Discard all unsaved employee changes?"))
        {
            Editor.DiscardChanges();
            ResetForm();
            _error = null;
            _message = "Unsaved changes discarded.";
        }
    }

    private async Task ReloadAsync()
    {
        if (_busy)
        {
            return;
        }

        if (Editor.HasChanges && !await JS.InvokeAsync<bool>("confirm", "Refresh and discard all unsaved changes?"))
        {
            return;
        }

        await LoadAsync();
    }

    private async Task BeforeNavigationAsync(LocationChangingContext context)
    {
        if (_busy || (Editor.HasChanges && !await JS.InvokeAsync<bool>("confirm", "Leave this page and discard unsaved changes?")))
        {
            context.PreventNavigation();
        }
    }

    private async Task RunAsync(Func<Task> operation)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        _error = null;
        _message = null;

        try
        {
            await operation();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Employee workspace operation failed.");

            _error = exception switch
            {
                ArgumentException => exception.Message,
                DbUpdateConcurrencyException => "An employee no longer exists. Refresh the list before trying again.",
                DbUpdateException => "No changes were saved. Check for duplicate email addresses or departments that no longer exist.",
                _ => "The operation could not be completed. Check the database connection or see the application log."
            };
        }
        finally
        {
            _busy = false;
        }
    }
}
