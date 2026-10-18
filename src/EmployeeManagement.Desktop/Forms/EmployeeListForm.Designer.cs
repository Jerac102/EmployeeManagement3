#nullable enable

using EmployeeManagement.Shared.Models;

namespace EmployeeManagement.Desktop.Forms;

partial class EmployeeListForm
{
    private System.ComponentModel.IContainer? components;
    private DataGridView employeesGrid = null!;
    private ToolStrip toolbar = null!;
    private ToolStripButton addButton = null!;
    private ToolStripButton editButton = null!;
    private ToolStripButton deleteButton = null!;
    private ToolStripButton refreshButton = null!;
    private ToolStripButton saveButton = null!;
    private ToolStripButton discardButton = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _employeeEditor.Rows.ListChanged -= EmployeeRows_ListChanged;
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        employeesGrid = new DataGridView();
        toolbar = new ToolStrip();
        addButton = new ToolStripButton();
        editButton = new ToolStripButton();
        deleteButton = new ToolStripButton();
        refreshButton = new ToolStripButton();
        saveButton = new ToolStripButton();
        discardButton = new ToolStripButton();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();

        ((System.ComponentModel.ISupportInitialize)employeesGrid).BeginInit();
        toolbar.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        toolbar.Name = "toolbar";
        toolbar.Dock = DockStyle.Top;
        toolbar.GripStyle = ToolStripGripStyle.Hidden;
        toolbar.Items.AddRange([addButton, editButton, deleteButton, new ToolStripSeparator(), saveButton, discardButton, refreshButton]);

        addButton.Name = "addButton";
        addButton.Text = "New";
        addButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        addButton.Click += AddButton_Click;

        editButton.Name = "editButton";
        editButton.Text = "Edit";
        editButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        editButton.Click += EditButton_Click;

        deleteButton.Name = "deleteButton";
        deleteButton.Text = "Delete";
        deleteButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        deleteButton.Click += DeleteButton_Click;

        refreshButton.Name = "refreshButton";
        refreshButton.Text = "Refresh";
        refreshButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        refreshButton.Click += RefreshButton_Click;

        saveButton.Name = "saveButton";
        saveButton.Text = "Save All";
        saveButton.ToolTipText = "Save all pending changes (Ctrl+S)";
        saveButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        saveButton.Click += SaveButton_Click;

        discardButton.Name = "discardButton";
        discardButton.Text = "Discard";
        discardButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        discardButton.Click += DiscardButton_Click;

        employeesGrid.Name = "employeesGrid";
        employeesGrid.Dock = DockStyle.Fill;
        employeesGrid.AutoGenerateColumns = false;
        employeesGrid.ReadOnly = false;
        employeesGrid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        employeesGrid.AllowUserToAddRows = false;
        employeesGrid.AllowUserToDeleteRows = false;
        employeesGrid.MultiSelect = false;
        employeesGrid.RowHeadersVisible = false;
        employeesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        employeesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        employeesGrid.BackgroundColor = SystemColors.Window;
        employeesGrid.BorderStyle = BorderStyle.None;
        employeesGrid.SelectionChanged += EmployeesGrid_SelectionChanged;
        employeesGrid.CellFormatting += EmployeesGrid_CellFormatting;
        employeesGrid.CellBeginEdit += EmployeesGrid_CellBeginEdit;
        employeesGrid.CellValidating += EmployeesGrid_CellValidating;
        employeesGrid.CellParsing += EmployeesGrid_CellParsing;
        employeesGrid.CellEndEdit += EmployeesGrid_CellEndEdit;
        employeesGrid.CurrentCellDirtyStateChanged += EmployeesGrid_CurrentCellDirtyStateChanged;
        employeesGrid.DataError += EmployeesGrid_DataError;

        employeesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "idColumn",
            DataPropertyName = nameof(EmployeeRow.Id),
            HeaderText = "ID",
            ReadOnly = true,
            FillWeight = 35
        });

        employeesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "firstNameColumn",
            DataPropertyName = nameof(EmployeeRow.FirstName),
            HeaderText = "First name",
            MaxInputLength = 100
        });

        employeesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "lastNameColumn",
            DataPropertyName = nameof(EmployeeRow.LastName),
            HeaderText = "Last name",
            MaxInputLength = 100
        });

        employeesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "emailColumn",
            DataPropertyName = nameof(EmployeeRow.Email),
            HeaderText = "Email",
            MaxInputLength = 200,
            FillWeight = 160
        });

        employeesGrid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "departmentColumn",
            DataPropertyName = nameof(EmployeeRow.DepartmentId),
            HeaderText = "Department",
            DisplayMember = "Name",
            ValueMember = "Id",
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            FlatStyle = FlatStyle.Flat
        });

        employeesGrid.Columns.Add(new DateOnlyPickerColumn
        {
            Name = "hireDateColumn",
            DataPropertyName = nameof(EmployeeRow.HireDate),
            HeaderText = "Hire date",
            DefaultCellStyle = new DataGridViewCellStyle { Format = "d" }
        });

        statusStrip.Name = "statusStrip";
        statusStrip.Items.Add(statusLabel);
        statusLabel.Name = "statusLabel";
        statusLabel.Text = "Ready";

        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1050, 550);
        MinimumSize = new Size(750, 350);
        StartPosition = FormStartPosition.CenterScreen;
        Name = "EmployeeListForm";
        Text = "Employee Management";

        Controls.Add(employeesGrid);
        Controls.Add(toolbar);
        Controls.Add(statusStrip);

        ((System.ComponentModel.ISupportInitialize)employeesGrid).EndInit();
        toolbar.ResumeLayout(false);
        toolbar.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
