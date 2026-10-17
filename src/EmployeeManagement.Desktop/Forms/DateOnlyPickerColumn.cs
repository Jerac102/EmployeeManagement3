using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace EmployeeManagement.Desktop.Forms;

public sealed class DateOnlyPickerColumn : DataGridViewColumn
{
    public DateOnlyPickerColumn() : base(new DateOnlyPickerCell())
    {
        DefaultCellStyle.Format = "d";
    }
}

public sealed class DateOnlyPickerCell : DataGridViewTextBoxCell
{
    public override Type EditType => typeof(DateOnlyPickerEditingControl);

    public override Type ValueType => typeof(DateOnly);

    public override Type FormattedValueType => typeof(string);

    public override object DefaultNewRowValue => DateOnly.FromDateTime(DateTime.Today);

    public override void InitializeEditingControl(
        int rowIndex,
        object? initialFormattedValue,
        DataGridViewCellStyle dataGridViewCellStyle)
    {
        base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);

        if (DataGridView?.EditingControl is DateOnlyPickerEditingControl picker)
        {
            picker.SetDate(Value is DateOnly date ? date : DateOnly.FromDateTime(DateTime.Today));
        }
    }

    public override object ParseFormattedValue(
        object? formattedValue,
        DataGridViewCellStyle cellStyle,
        TypeConverter? formattedValueTypeConverter,
        TypeConverter? valueTypeConverter)
    {
        if (formattedValue is string text
            && DateOnly.TryParse(text, CultureInfo.CurrentCulture, out var date))
        {
            return date;
        }

        throw new FormatException("Enter a valid hire date.");
    }
}

public sealed class DateOnlyPickerEditingControl : DateTimePicker, IDataGridViewEditingControl
{
    private bool _initializing;
    private bool _calendarOpen;
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Today);

    public DateOnlyPickerEditingControl()
    {
        Format = DateTimePickerFormat.Short;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DataGridView? EditingControlDataGridView { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int EditingControlRowIndex { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool EditingControlValueChanged { get; set; }

    [AllowNull]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public object EditingControlFormattedValue
    {
        get => _selectedDate.ToString("d", CultureInfo.CurrentCulture);

        set
        {
            if (value is string text && DateOnly.TryParse(text, CultureInfo.CurrentCulture, out var date))
            {
                SetDate(date);
            }
        }
    }

    public Cursor EditingPanelCursor => Cursors.Default;

    public bool RepositionEditingControlOnValueChange => false;

    public void SetDate(DateOnly date)
    {
        _initializing = true;

        try
        {
            _selectedDate = date;

            var value = date.ToDateTime(TimeOnly.MinValue);

            // Preserve legacy dates outside the native picker's range unless the user changes them.
            Value = value < MinDate || value > MaxDate ? DateTime.Today : value;
            EditingControlValueChanged = false;
        }
        finally
        {
            _initializing = false;
        }
    }

    public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
    {
        return EditingControlFormattedValue;
    }

    public void ApplyCellStyleToEditingControl(DataGridViewCellStyle cellStyle)
    {
        Font = cellStyle.Font;
        CalendarForeColor = cellStyle.ForeColor;
        CalendarMonthBackground = cellStyle.BackColor;
    }

    public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
    {
        return (keyData & Keys.KeyCode) switch
        {
            Keys.Left or Keys.Right or Keys.Up or Keys.Down
                or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown or Keys.F4 => true,
            Keys.Escape or Keys.Enter when _calendarOpen => true,
            _ => !dataGridViewWantsInputKey
        };
    }

    public void PrepareEditingControlForEdit(bool selectAll)
    {
        // The native picker handles date-segment selection.
    }

    protected override void OnValueChanged(EventArgs eventargs)
    {
        base.OnValueChanged(eventargs);

        if (!_initializing)
        {
            _selectedDate = DateOnly.FromDateTime(Value);
            EditingControlValueChanged = true;

            EditingControlDataGridView?.NotifyCurrentCellDirty(true);
        }
    }

    protected override void OnDropDown(EventArgs eventargs)
    {
        _calendarOpen = true;

        base.OnDropDown(eventargs);
    }

    protected override void OnCloseUp(EventArgs eventargs)
    {
        _calendarOpen = false;

        base.OnCloseUp(eventargs);
    }
}
