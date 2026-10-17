using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using EmployeeManagement.Data;
using EmployeeManagement.Data.Data;
using EmployeeManagement.Data.Entities;
using EmployeeManagement.Data.Services;
using EmployeeManagement.Desktop.Editing;
using EmployeeManagement.Desktop.Forms;
using EmployeeManagement.Desktop.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmployeeManagement.Desktop.Tests;

[TestClass]
public class EmployeeFormTests
{
    private static void RunOnUiThread(Action action)
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        }) { IsBackground = true };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(15)), "UI test did not finish.");

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static void WithLoadedForm(Action<EmployeeListForm, EmployeeEditor, DataGridView> action)
    {
        RunOnUiThread(() =>
        {
            using var context = new EmployeeManagementDbContext(
                new DbContextOptionsBuilder<EmployeeManagementDbContext>().Options);

            var editor = new EmployeeEditor(new EmployeeManagementService(context));
            var departments = new List<Department>
            {
                new() { Id = 1, Name = "IT" },
                new() { Id = 2, Name = "Finance" }
            };

            typeof(EmployeeEditor).GetProperty(nameof(EmployeeEditor.Departments))!
                .SetValue(editor, departments);

            editor.Rows.Add(EmployeeRow.FromData(
                new EmployeeData(5, "Max", "Sample", null, new DateOnly(2024, 1, 15), 2)));

            using var form = new PreviewEmployeeListForm(editor);

            typeof(EmployeeListForm).GetField("_isLoaded", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, true);

            var grid = (DataGridView)form.Controls.Find("employeesGrid", true).Single();

            ((DataGridViewComboBoxColumn)grid.Columns["departmentColumn"]!).DataSource = departments;

            form.ShowInTaskbar = false;
            form.Opacity = 0;
            form.Show();
            Application.DoEvents();

            action(form, editor, grid);
        });
    }

    [TestMethod]
    public void GridEdits_UpdateRowAndKeepDatabaseIdReadOnly()
    {
        WithLoadedForm((form, editor, grid) =>
        {
            Assert.IsTrue(grid.Columns["idColumn"]!.ReadOnly);

            grid.CurrentCell = grid.Rows[0].Cells["firstNameColumn"];

            Assert.IsTrue(grid.BeginEdit(true));

            ((TextBox)grid.EditingControl!).Text = "Updated";

            Assert.IsTrue(grid.EndEdit());
            Assert.AreEqual("Updated", editor.Rows[0].FirstName);
            Assert.IsTrue(editor.HasChanges);

            var toolbar = (ToolStrip)form.Controls.Find("toolbar", true).Single();

            Assert.IsTrue(toolbar.Items["saveButton"]!.Enabled);
        });
    }

    [TestMethod]
    public void DateAndDepartment_CanBeEditedInGrid()
    {
        WithLoadedForm((form, editor, grid) =>
        {
            var date = new DateOnly(2025, 2, 20);

            grid.CurrentCell = grid.Rows[0].Cells["hireDateColumn"];

            Assert.IsTrue(grid.BeginEdit(true));

            ((DateTimePicker)grid.EditingControl!).Value = date.ToDateTime(TimeOnly.MinValue);

            Assert.IsTrue(grid.EndEdit());
            Assert.AreEqual(date, editor.Rows[0].HireDate);

            grid.CurrentCell = grid.Rows[0].Cells["departmentColumn"];

            Assert.IsTrue(grid.BeginEdit(true));

            ((ComboBox)grid.EditingControl!).SelectedValue = 1;

            Assert.IsTrue(grid.EndEdit());
            Assert.AreEqual(1, editor.Rows[0].DepartmentId);

            using var bitmap = new Bitmap(form.Width, form.Height);

            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
            bitmap.Save(Path.Combine(AppContext.BaseDirectory, "EmployeeListForm.png"));
        });
    }

    [TestMethod]
    public void DatePicker_CanCancelWithoutChangingTheEmployee()
    {
        WithLoadedForm((form, editor, grid) =>
        {
            var originalDate = editor.Rows[0].HireDate;

            grid.CurrentCell = grid.Rows[0].Cells["hireDateColumn"];

            Assert.IsTrue(grid.BeginEdit(true));
            Assert.IsInstanceOfType<DateTimePicker>(grid.EditingControl);
            Assert.IsFalse(editor.HasChanges);

            ((DateTimePicker)grid.EditingControl!).Value = new DateTime(2025, 3, 10);

            grid.CancelEdit();

            Assert.AreEqual(originalDate, editor.Rows[0].HireDate);
            Assert.IsFalse(editor.HasChanges);
        });
    }

    [TestMethod]
    public void DatePicker_OpeningAndLeavingPreservesTheOriginalDate()
    {
        WithLoadedForm((form, editor, grid) =>
        {
            var originalDate = editor.Rows[0].HireDate;

            grid.CurrentCell = grid.Rows[0].Cells["hireDateColumn"];

            Assert.IsTrue(grid.BeginEdit(true));
            Assert.AreEqual(originalDate.ToDateTime(TimeOnly.MinValue), ((DateTimePicker)grid.EditingControl!).Value);
            Assert.IsTrue(grid.EndEdit());
            Assert.AreEqual(originalDate, editor.Rows[0].HireDate);
            Assert.IsFalse(editor.HasChanges);
        });
    }

    [TestMethod]
    public void New_AddsRowWithDefaultDepartmentAndStartsCellEdit()
    {
        WithLoadedForm((form, editor, grid) =>
        {
            var toolbar = (ToolStrip)form.Controls.Find("toolbar", true).Single();

            toolbar.Items["addButton"]!.PerformClick();

            Assert.HasCount(2, editor.Rows);
            Assert.IsTrue(editor.Rows[1].IsNew);
            Assert.AreEqual(1, editor.Rows[1].DepartmentId);
            Assert.AreEqual("firstNameColumn", grid.CurrentCell!.OwningColumn!.Name);
            Assert.IsTrue(grid.IsCurrentCellInEditMode);

            grid.CancelEdit();
        });
    }

    [TestMethod]
    public void DeletedRows_CannotBeEditedUntilDeletionIsUndone()
    {
        WithLoadedForm((form, editor, grid) =>
        {
            editor.MarkForDelete(editor.Rows[0]);
            grid.CurrentCell = grid.Rows[0].Cells["firstNameColumn"];

            Assert.IsFalse(grid.BeginEdit(true));

            var toolbar = (ToolStrip)form.Controls.Find("toolbar", true).Single();

            Assert.AreEqual("Undo delete", toolbar.Items["deleteButton"]!.Text);

            toolbar.Items["deleteButton"]!.PerformClick();

            Assert.IsFalse(editor.Rows[0].IsMarkedForDelete);
            Assert.IsTrue(grid.BeginEdit(true));

            grid.CancelEdit();
        });
    }

    private sealed class PreviewEmployeeListForm : EmployeeListForm
    {
        public PreviewEmployeeListForm(EmployeeEditor editor)
            : base(editor, NullLogger<EmployeeListForm>.Instance)
        {
        }

        protected override void OnShown(EventArgs e)
        {
            // The tests supply loaded data without starting a database operation.
        }
    }
}
