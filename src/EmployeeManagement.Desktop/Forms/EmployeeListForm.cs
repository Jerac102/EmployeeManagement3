using System.ComponentModel;
using System.Globalization;
using EmployeeManagement.Shared.Editing;
using EmployeeManagement.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Desktop.Forms;

public partial class EmployeeListForm : Form
{
    private readonly EmployeeEditor _employeeEditor;
    private readonly ILogger<EmployeeListForm> _logger;
    private bool _isBusy;
    private bool _isLoaded;
    private bool _closeConfirmed;

    public EmployeeListForm(EmployeeEditor employeeEditor, ILogger<EmployeeListForm> logger)
    {
        _employeeEditor = employeeEditor;
        _logger = logger;

        InitializeComponent();

        employeesGrid.DataSource = _employeeEditor.Rows;
        _employeeEditor.Rows.ListChanged += EmployeeRows_ListChanged;

        UpdateButtons();
    }

    private EmployeeRow? SelectedEmployee => employeesGrid.CurrentRow?.DataBoundItem as EmployeeRow;

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);

        await LoadEmployeesAsync();
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        if (e.Cancel || _closeConfirmed)
        {
            return;
        }

        // Keep the form's scoped context alive until the current operation completes.
        if (_isBusy)
        {
            e.Cancel = true;

            return;
        }

        if (!CommitPendingEdit())
        {
            e.Cancel = true;

            return;
        }

        if (!_employeeEditor.HasChanges)
        {
            return;
        }

        e.Cancel = true;

        var answer = MessageBox.Show(
            this,
            "Save all changes before closing? Choose No to discard them.",
            "Unsaved changes",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);

        if (answer == DialogResult.Cancel)
        {
            return;
        }

        if (answer == DialogResult.Yes && !await SaveAllAsync())
        {
            return;
        }

        _closeConfirmed = true;

        Close();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.S))
        {
            if (saveButton.Enabled)
            {
                SaveButton_Click(this, EventArgs.Empty);
            }

            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private async Task LoadEmployeesAsync()
    {
        if (_isBusy)
        {
            return;
        }

        SetBusy(true);
        statusLabel.Text = "Loading employees...";

        try
        {
            await _employeeEditor.LoadAsync();

            var departmentColumn = (DataGridViewComboBoxColumn)employeesGrid.Columns["departmentColumn"]!;

            departmentColumn.DataSource = _employeeEditor.Departments.ToList();
            _isLoaded = true;

            employeesGrid.Invalidate();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Employee list could not be loaded.");

            MessageBox.Show(
                this,
                "Employees could not be loaded. Check the database configuration and try Refresh. See the log for details.",
                "Load failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task<bool> SaveAllAsync()
    {
        if (_isBusy)
        {
            return false;
        }

        if (!CommitPendingEdit())
        {
            return false;
        }

        if (!_employeeEditor.HasChanges)
        {
            return true;
        }

        SetBusy(true);
        statusLabel.Text = "Saving all changes...";

        try
        {
            await _employeeEditor.SaveAllAsync();

            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "The employee editing session could not be saved.");

            var message = exception switch
            {
                ArgumentException => exception.Message,
                InvalidOperationException => exception.Message,
                DbUpdateConcurrencyException => "No changes were saved because an employee no longer exists. Refresh the list before trying again.",
                DbUpdateException => "No changes were saved. Check for duplicate email addresses or departments that no longer exist.",
                _ => "No changes were saved. See the application log for details."
            };

            MessageBox.Show(this, message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);

            return false;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
        employeesGrid.Enabled = !isBusy;

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var canEdit = !_isBusy && _isLoaded;
        var selected = SelectedEmployee;

        addButton.Enabled = canEdit && _employeeEditor.Departments.Count > 0;
        editButton.Enabled = canEdit && selected is { IsMarkedForDelete: false };
        deleteButton.Enabled = canEdit && selected is not null;
        deleteButton.Text = selected is { IsMarkedForDelete: true } ? "Undo delete" : "Delete";
        saveButton.Enabled = canEdit && (_employeeEditor.HasChanges || employeesGrid.IsCurrentCellDirty);
        discardButton.Enabled = canEdit && (_employeeEditor.HasChanges || employeesGrid.IsCurrentCellDirty);
        refreshButton.Enabled = !_isBusy;

        if (!_isBusy)
        {
            statusLabel.Text = !_isLoaded
                ? "Use Refresh to load employees."
                : $"{_employeeEditor.Rows.Count} employee(s) | {_employeeEditor.ChangedRowCount} pending change(s)";

            if (_isLoaded && _employeeEditor.Departments.Count == 0)
            {
                statusLabel.Text += " | No departments available; create or seed a department first.";
            }
        }
    }

    private void EmployeeRows_ListChanged(object? sender, ListChangedEventArgs e)
    {
        UpdateButtons();
        employeesGrid.Invalidate();
    }

    private async void RefreshButton_Click(object? sender, EventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        if ((_employeeEditor.HasChanges || employeesGrid.IsCurrentCellDirty) && !ConfirmDiscard())
        {
            return;
        }

        employeesGrid.CancelEdit();

        await LoadEmployeesAsync();
    }

    private bool ConfirmDiscard()
    {
        return MessageBox.Show(
            this,
            "Discard all unsaved employee changes?",
            "Discard changes",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) == DialogResult.Yes;
    }

    private void EmployeesGrid_SelectionChanged(object? sender, EventArgs e)
    {
        UpdateButtons();
    }

    private void AddButton_Click(object? sender, EventArgs e)
    {
        if (_isBusy || !_isLoaded || _employeeEditor.Departments.Count == 0 || !CommitPendingEdit())
        {
            return;
        }

        var row = _employeeEditor.AddEmployee();

        SelectRow(row);

        employeesGrid.BeginEdit(true);
    }

    private void EditButton_Click(object? sender, EventArgs e)
    {
        var row = SelectedEmployee;

        if (_isBusy || row is null || row.IsMarkedForDelete)
        {
            return;
        }

        if (employeesGrid.CurrentCell is null || employeesGrid.CurrentCell.ReadOnly)
        {
            SelectRow(row);
        }

        employeesGrid.Focus();
        employeesGrid.BeginEdit(true);
    }

    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        var row = SelectedEmployee;

        if (_isBusy || row is null)
        {
            return;
        }

        if (row.IsMarkedForDelete)
        {
            row.IsMarkedForDelete = false;

            return;
        }

        if (MessageBox.Show(
            this,
            $"Mark {row.FirstName} {row.LastName} for deletion? It will be removed from the database when you Save All.",
            "Delete employee",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            employeesGrid.CancelEdit();

            _employeeEditor.MarkForDelete(row);
        }
    }

    private async void SaveButton_Click(object? sender, EventArgs e)
    {
        await SaveAllAsync();
    }

    private void DiscardButton_Click(object? sender, EventArgs e)
    {
        if (!_isBusy && (_employeeEditor.HasChanges || employeesGrid.IsCurrentCellDirty) && ConfirmDiscard())
        {
            employeesGrid.CancelEdit();
            _employeeEditor.DiscardChanges();

            foreach (DataGridViewRow row in employeesGrid.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    cell.ErrorText = string.Empty;
                }
            }

            UpdateButtons();
        }
    }

    private bool CommitPendingEdit()
    {
        if (!employeesGrid.EndEdit())
        {
            statusLabel.Text = "Correct the highlighted cell before saving or closing. Escape cancels the cell edit.";

            return false;
        }

        BindingContext?[_employeeEditor.Rows].EndCurrentEdit();

        return true;
    }

    private void SelectRow(EmployeeRow row)
    {
        foreach (DataGridViewRow gridRow in employeesGrid.Rows)
        {
            if (ReferenceEquals(gridRow.DataBoundItem, row))
            {
                employeesGrid.CurrentCell = gridRow.Cells["firstNameColumn"];

                break;
            }
        }
    }

    private void EmployeesGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || employeesGrid.Rows[e.RowIndex].DataBoundItem is not EmployeeRow row)
        {
            return;
        }

        if (e.CellStyle is not null)
        {
            e.CellStyle.BackColor = row.IsMarkedForDelete ? Color.MistyRose
                : row.IsNew ? Color.Honeydew
                : row.HasChanges ? Color.LemonChiffon
                : employeesGrid.DefaultCellStyle.BackColor;

            e.CellStyle.ForeColor = row.IsMarkedForDelete ? Color.DimGray : employeesGrid.DefaultCellStyle.ForeColor;
        }
    }
    private void EmployeesGrid_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        if (_isBusy || !_isLoaded
            || employeesGrid.Rows[e.RowIndex].DataBoundItem is EmployeeRow { IsMarkedForDelete: true })
        {
            e.Cancel = true;
        }
    }

    private void EmployeesGrid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        var cell = employeesGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        var name = employeesGrid.Columns[e.ColumnIndex].Name;
        var text = Convert.ToString(e.FormattedValue, CultureInfo.CurrentCulture) ?? string.Empty;

        var error = name switch
        {
            "firstNameColumn" or "lastNameColumn" when string.IsNullOrWhiteSpace(text)
                => "A name is required.",
            "hireDateColumn" when !DateOnly.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out _)
                => "Enter a valid date using your regional date format.",
            _ => string.Empty
        };

        cell.ErrorText = error;
        e.Cancel = error.Length > 0;
    }

    private void EmployeesGrid_CellParsing(object? sender, DataGridViewCellParsingEventArgs e)
    {
        if (employeesGrid.Columns[e.ColumnIndex].Name == "hireDateColumn"
            && DateOnly.TryParse(Convert.ToString(e.Value, CultureInfo.CurrentCulture),
                CultureInfo.CurrentCulture, DateTimeStyles.None, out var date))
        {
            e.Value = date;
            e.ParsingApplied = true;
        }
    }

    private void EmployeesGrid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (employeesGrid.IsCurrentCellDirty && employeesGrid.CurrentCell is DataGridViewComboBoxCell)
        {
            employeesGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        UpdateButtons();
    }

    private void EmployeesGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        employeesGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText = string.Empty;

        UpdateButtons();
    }

    private void EmployeesGrid_DataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
        e.Cancel = true;

        if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            employeesGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText = "Check this value.";
        }
    }
}
