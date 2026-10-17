using EmployeeManagement.Data;
using EmployeeManagement.Data.Data;
using EmployeeManagement.Data.Services;
using EmployeeManagement.Desktop.Editing;
using EmployeeManagement.Desktop.Models;
using Microsoft.EntityFrameworkCore;

[assembly: DoNotParallelize]

namespace EmployeeManagement.Desktop.Tests;

[TestClass]
public class EmployeeEditorTests
{
    private EmployeeManagementDbContext _context = null!;
    private EmployeeEditor _editor = null!;

    [TestInitialize]
    public void SetUp()
    {
        // These editing tests do not query the database.
        _context = new EmployeeManagementDbContext(
            new DbContextOptionsBuilder<EmployeeManagementDbContext>().Options);

        _editor = new EmployeeEditor(new EmployeeManagementService(_context));
    }

    [TestCleanup]
    public void TearDown()
    {
        _context.Dispose();
    }

    private static EmployeeRow ExistingRow(int id)
    {
        return EmployeeRow.FromData(
            new EmployeeData(id, "Max", "Sample", null, new DateOnly(2024, 1, 15), 1));
    }

    [TestMethod]
    public void CreateChanges_SeparatesNewModifiedAndDeletedEmployees()
    {
        var unchanged = ExistingRow(1);
        var modified = ExistingRow(2);
        var deleted = ExistingRow(3);

        _editor.Rows.Add(unchanged);
        _editor.Rows.Add(modified);
        _editor.Rows.Add(deleted);

        var added = _editor.AddEmployee();

        added.FirstName = "New";
        modified.LastName = "Changed";
        deleted.FirstName = "Edited before deletion";

        _editor.MarkForDelete(deleted);

        var changes = _editor.CreateChanges();

        Assert.AreEqual(added.ToData(), changes.NewEmployees.Single());
        Assert.AreEqual(modified.ToData(), changes.ModifiedEmployees.Single());
        Assert.AreEqual(deleted.Id, changes.DeletedEmployeeIds.Single());
        Assert.AreEqual(3, _editor.ChangedRowCount);
        Assert.IsTrue(modified.HasChanges);
    }

    [TestMethod]
    public void DeleteNewEmployee_RemovesRowWithoutDatabaseDeletion()
    {
        var added = _editor.AddEmployee();

        _editor.MarkForDelete(added);

        Assert.IsEmpty(_editor.Rows);
        Assert.IsEmpty(_editor.CreateChanges().DeletedEmployeeIds);
        Assert.IsFalse(_editor.HasChanges);
    }

    [TestMethod]
    public void DiscardChanges_RestoresOriginalValuesAndRemovesNewRows()
    {
        var modified = ExistingRow(1);
        var deleted = ExistingRow(2);

        _editor.Rows.Add(modified);
        _editor.Rows.Add(deleted);
        _editor.AddEmployee();

        modified.FirstName = "Changed";
        _editor.MarkForDelete(deleted);

        _editor.DiscardChanges();

        Assert.HasCount(2, _editor.Rows);
        Assert.AreEqual("Max", modified.FirstName);
        Assert.IsFalse(deleted.IsMarkedForDelete);
        Assert.IsFalse(_editor.HasChanges);
    }

    [TestMethod]
    public void RevertedEdit_IsExcludedFromChanges()
    {
        var row = ExistingRow(1);

        _editor.Rows.Add(row);

        row.FirstName = "Changed";
        row.FirstName = "Max";

        Assert.IsEmpty(_editor.CreateChanges().ModifiedEmployees);
        Assert.IsFalse(_editor.HasChanges);
    }
}