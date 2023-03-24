using System.Globalization;

using GenderNameEstimator.Tools;
using GenderNameEstimator.Tools.Csv;
using GenderNameEstimator.Tools.Xlsx;

namespace GenderNameEstimator.UI.Mac;

// TODO: Add setting for SpreadsheetColumnChooserViewController for just viewing the data?
public partial class SpreadsheetColumnChooserViewController : WizardOutlineViewController
{
    private SpreadsheetColumnChooserViewModel? _model;

    private bool _needsTableRefresh;
    private bool _needsMenuRefresh;

    public static SpreadsheetColumnChooserViewController Create(SpreadsheetColumnChooserViewModel? model)
    {
        var result = Instantiate<SpreadsheetColumnChooserViewController>();
        result.Model = model;
        return result;
    }

    public SpreadsheetColumnChooserViewController(ObjCRuntime.NativeHandle handle) : base(handle)
    {
    }

    public override void AwakeFromNib()
    {
        base.AwakeFromNib();
        RefreshTableView();
    }

    private void RefreshTableView()
    {
        RefreshTableView(null);
    }

    private TextRecordReader? GetReader()
    {
        // TODO: Data bind WorksheetDropDown instead and have the list of worksheets available from the reader (if applicable)
        return _model?.GetReader(WorksheetDropDown);
    }

    partial void WorksheetChanged(NSObject sender)
    {
        if (_model is null)
        {
            return;
        }
        _model.Worksheet = (int)WorksheetDropDown.SelectedIndex;
    }

    partial void RefreshTableView(NSObject sender)
    {
        // TODO: Automatic refresh with filesystem watcher
        if (TableView is null || !ViewLoaded || View.Window is null)
        {
            _needsTableRefresh = true;
            return;
        }

        var selectedColumns = _model?.Columns.AsEnumerable().Select(column => column.SelectedIndex).ToArray() ?? Array.Empty<int>();

        TableView.DataSource = null;
        TableView.Delegate = null;

        // We need this to ensure that we highlight things properly
        var selectedColumnChanges = _model?.Columns.AsEnumerable().Select(column => column.ChangeSection(m => m.SelectedIndex)).ToArray() ?? Array.Empty<IDisposable>();
        try
        {
            foreach (var column in TableView.TableColumns().Where(c => c != RowColumn))
            {
                TableView.RemoveColumn(column);
                column.Dispose();
            }

            try
            {
                // TODO: Asynchronous?
                using var reader = GetReader();
                if (reader is null)
                {
                    return;
                }

                if (reader.Headers is not null)
                {
                    int columnIndex = 0;
                    foreach (var title in reader.Headers.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key))
                    {
                        TableView.AddColumn(new NSTableColumn(columnIndex++.ToString(CultureInfo.InvariantCulture))
                        {
                            Title = title,
                            Editable = false,
                        });
                    }
                }

                var dataSource = new WorksheetDataSource(reader);
                TableView.DataSource = dataSource;
                TableView.Delegate = new ListOfStringArrayViewDelegate();

                if (_model is not null)
                {
                    for (nuint i = 0; i < _model.Columns.Count; i++)
                    {
                        var selectedColumn = selectedColumns[i];
                        _model.Columns[i].SelectedIndex = (reader.Headers?.Count ?? -1) > selectedColumn
                            ? selectedColumn
                            : -1;
                    }
                }
                _needsTableRefresh = false;
            }
            catch (Exception exception)
            {
                using var alert = new NSAlert
                {
                    AlertStyle = NSAlertStyle.Warning,
                    MessageText = "Could Not Open Spreadsheet",
                    InformativeText = exception.Message
                };
                alert.RunModal();

                if (_model is not null)
                {
                    foreach (var column in _model.Columns)
                    {
                        column.SelectedIndex = -1;
                    }
                }
            }
        }
        finally
        {
            foreach (var disposable in selectedColumnChanges)
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                    // Ignored
                }
            }
        }
    }

    public override void ViewDidAppear()
    {
        if (_needsTableRefresh)
        {
            RefreshTableView();
        }
        if (_needsMenuRefresh)
        {
            RefreshContextMenu();
        }
        base.ViewDidAppear();
    }

    private void RefreshContextMenu()
    {
        if (ContextMenu is null || TableView is null || !ViewLoaded || View.Window is null)
        {
            _needsMenuRefresh = true;
            return;
        }

        if (ContextMenu.Delegate is null)
        {
            ContextMenu.Delegate = new MenuDelegate(TableView);
        }

        // TODO: Data binding instead of hard-coded buiulding of the menu items
        ContextMenu.RemoveAllItems();
        if (_model is not null)
        {
            foreach (var column in _model.Columns)
            {
                ContextMenu.AddItem(new NSMenuItem(column.Purpose, ContextMenuItem_Selected)
                {
                    RepresentedObject = column
                });
            }
        }
        _needsMenuRefresh = false;
    }

    private void ContextMenuItem_Selected(object? sender, EventArgs e)
    {
        if (_model is null || sender is not NSMenuItem menuItem || menuItem.RepresentedObject is not ColumnInfo columnInfo)
        {
            // Isn't really possible
            return;
        }

        var tableColumnIndex = TableView.ClickedColumn;
        if (tableColumnIndex < 0)
        {
            tableColumnIndex = TableView.Tag;
        }
        TableView.Tag = -1;
        if (tableColumnIndex < 0)
        {
            // TODO: This happens when we right click a column header, which we disabled in the subclass for now
            return;
        }
        var tableColumns = TableView.TableColumns();
        var tableColumn = tableColumns[tableColumnIndex];
        if (tableColumn.Identifier == "RowColumn")
        {
            // TODO: Prevent the menu from showing up?
            return;
        }

        var newSpreadsheetColumnIndex = int.Parse(tableColumn.Identifier, CultureInfo.InvariantCulture);

        if (columnInfo.SelectedIndex == newSpreadsheetColumnIndex)
        {
            // TODO: Make the already selected column unlickable in the menu
            return;
        }

        if (columnInfo.SelectedIndex >= 0)
        {
            // Unmark the old table column
            for (int i = 0; i < tableColumns.Length; i++)
            {
                if (int.TryParse(tableColumns[i].Identifier, out var n) && n == columnInfo.SelectedIndex)
                {
                    tableColumns[i].HeaderToolTip = "";
                    TableView.DeselectColumn((nint)i);
                }
            }
        }

        // If the new table column was already assigned to something else, clear it
        var oldColumnInfo = _model.Columns.AsEnumerable().FirstOrDefault(c => c.SelectedIndex == newSpreadsheetColumnIndex);
        if (oldColumnInfo is not null)
        {
            oldColumnInfo.SelectedIndex = -1;
        }

        columnInfo.SelectedIndex = newSpreadsheetColumnIndex;
        tableColumn.HeaderToolTip = columnInfo.Purpose;
        TableView.SelectColumn(tableColumnIndex, true);
    }

    [Export(nameof(Model))]
    public SpreadsheetColumnChooserViewModel? Model
    {
        get => _model;
        set
        {
            if (this.ChangeField(ref _model, value, t => t.Model, t => t.IsEnabled, t => t.IsCompleted))
            {
                if (_model is not null)
                {
                    AddObservers(this.Properties(t => t.IsEnabled).DependOn(_model.Properties(m => m.Filename)));
                    AddObservers(this.Properties(t => t.IsCompleted).DependOn(_model.Properties(m => m.Columns)));

                    // TODO: Manage destroying observers if the column is removed?  For the most part, we expect the column info to be fairly static, and the observers are cleaned up when the view controller goes away
                    // TODO: We could just wrap a collection of observer links along with the source and target to be disposed when either the source or target are disposed
                    AddObservers(_model.WhenPropertiesChange(
                        () => AddObservers(this.Properties(t => t.IsEnabled).DependOn(
                            _model.Columns.AsEnumerable().PropertiesOfAny(c => c.IsRequired, c => c.SelectedIndex).ToArray())),
                        m => m.Columns));
                    AddObservers(this.Properties(t => t.IsEnabled).DependOn(_model.Columns.AsEnumerable().PropertiesOfAny(c => c.IsRequired, c => c.SelectedIndex).ToArray()));

                    AddObservers(_model.WhenPropertiesChange(RefreshTableView, m => m.RowsToSkip, m => m.RowsToTrim, m => m.Filename, m => m.Worksheet));
                    AddObservers(_model.WhenPropertiesChange(RefreshContextMenu, m => m.Columns));
                    // NOTE: Selection changes are handled in the context menu interaction, which is the only place where they should be changed
                    AddObservers(_model.Columns.AsEnumerable().SelectMany(column => column.WhenPropertiesChange(RefreshContextMenu, c => c.Purpose)));
                }
                RefreshTableView();
                RefreshContextMenu();
            };
        }
    }

    public override bool IsCompleted => _model is not null && _model.Columns.AsEnumerable().All(column => !column.IsRequired || column.SelectedIndex >= 0);

    public override bool IsEnabled => !string.IsNullOrWhiteSpace(_model?.Filename);

#pragma warning disable CA1822 // Mark members as static
    partial void TableViewSelected(NSTableView sender)
#pragma warning restore CA1822 // Mark members as static
    {
        // Hack
        sender.Tag = sender.ClickedColumn;
    }

    partial void ViewInFinder(NSObject sender)
    {
        if (string.IsNullOrEmpty(_model?.Filename))
        {
            return;
        }
        using var url = NSUrl.CreateFileUrl(_model.Filename, null);
        NSWorkspace.SharedWorkspace.ActivateFileViewer(new NSUrl[] { url });
    }

    // TODO: Add special handling for hyperlinks in spreadsheet values?
    private class WorksheetDataSource : NSTableViewDataSource
    {
        private readonly int _firstRowIndex;

        public WorksheetDataSource(TextRecordReader reader)
        {
            _firstRowIndex = reader.CurrentRowIndex + 1;

            var data = new List<string[]>();
            foreach (var record in reader)
            {
                data.Add(record.GetValues());
            }
            Data = data.AsReadOnly();
        }

        public IList<string[]> Data { get; }

        public override nint GetRowCount(NSTableView tableView)
        {
            return Data.Count;
        }

        public override NSObject GetObjectValue(NSTableView tableView, NSTableColumn tableColumn, nint row)
        {
            if (tableColumn.Identifier == "RowColumn")
            {
                return new NSNumber(row + _firstRowIndex);
            }
            return new NSString(Data[(int)row][int.Parse(tableColumn.Identifier, CultureInfo.InvariantCulture)]);
        }
    }

    private class ListOfStringArrayViewDelegate : NSTableViewDelegate
    {
        public ListOfStringArrayViewDelegate()
        {
        }

        public override NSView GetViewForItem(NSTableView tableView, NSTableColumn tableColumn, nint row)
        {
            // TODO: How do we have the template predefined?
            // TODO: How do we autosize the height of the rows or widths of the columns?
            NSTextField view;
            if (tableColumn.Identifier == "RowColumn")
            {
                view = (NSTextField)tableView.MakeView("RowNumberView", this);
                if (view is null)
                {
                    view = new NSTextField
                    {
                        Identifier = "RowNumberView",
                        BackgroundColor = NSColor.Clear,
                        Bordered = false,
                        Selectable = true,
                        Editable = false,
                        Alignment = NSTextAlignment.Right,
                    };
                }
            }
            else
            {
                view = (NSTextField)tableView.MakeView("CellTemplate", this);
                if (view is null)
                {
                    view = new NSTextField
                    {
                        Identifier = "CellTemplate",
                        BackgroundColor = NSColor.Clear,
                        Bordered = false,
                        Selectable = true,
                        Editable = false,
                        Alignment = NSTextAlignment.Left
                    };
                }
            }
            return view;
        }

        public override bool ShouldSelectRow(NSTableView tableView, nint row)
        {
            return false;
        }

        public override bool ShouldSelectTableColumn(NSTableView tableView, NSTableColumn tableColumn)
        {
            return true;
        }
    }

    private class MenuDelegate : NSMenuDelegate
    {
        private readonly NSTableView _tableView;

        public MenuDelegate(NSTableView tableView)
        {
            _tableView = tableView;
        }

        public override void MenuWillOpen(NSMenu menu)
        {
            var index = _tableView.ClickedColumn >= 0 ? _tableView.ClickedColumn : _tableView.Tag;
            if (index >= 0 && int.TryParse(_tableView.TableColumns()[index].Identifier, out var selectedIndex))
            {
                foreach (var item in menu.Items)
                {
                    if (item.RepresentedObject is ColumnInfo info && info.SelectedIndex == selectedIndex)
                    {
                        item.State = NSCellStateValue.On;
                        item.Enabled = false;
                    }
                    else
                    {
                        item.State = NSCellStateValue.Off;
                        item.Enabled = true;
                    }
                }
            }
        }
    }
}
[Register(nameof(ColumnInfo))]
public class ColumnInfo : NSObject
{
    public static ColumnInfo Empty { get; } = new("", false);

    private string _purpose = "";
    private int _selectedIndex = -1;
    private bool _isRequired = false;

    public ColumnInfo(string purpose, bool isRequired = false)
    {
        Purpose = purpose;
        IsRequired = isRequired;
    }

    [Export(nameof(SelectedIndex))]
    public int SelectedIndex
    {
        get
        {
            return _selectedIndex;
        }
        set
        {
            this.ChangeField(ref _selectedIndex, value, t => t.SelectedIndex);
        }
    }

    [Export(nameof(Purpose))]
    public string Purpose
    {
        get
        {
            return _purpose;
        }
        set
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            this.ChangeStringField(ref _purpose, value, t => t.Purpose);
#pragma warning restore CS8601 // Possible null reference assignment.
        }
    }

    [Export(nameof(IsRequired))]
    public bool IsRequired
    {
        get
        {
            return _isRequired;
        }
        set
        {
            this.ChangeField(ref _isRequired, value, t => t.IsRequired);
        }
    }
}

[Register(nameof(SpreadsheetColumnChooserViewModel))]
public class SpreadsheetColumnChooserViewModel : NSObject
{
    private int _rowsToSkip;
    private int _rowsToTrim;
    private int _worksheet = -1;
    private string? _filename;

    public SpreadsheetColumnChooserViewModel()
    {
    }

    public SpreadsheetColumnChooserViewModel(SpreadsheetColumnChooserViewModel template)
    {
        _rowsToSkip = template.RowsToSkip;
        _rowsToTrim = template.RowsToTrim;
        _worksheet = template.Worksheet;
        _filename = template.Filename;
    }

    [Export(nameof(RowsToSkip))]
    public int RowsToSkip
    {
        get
        {
            return _rowsToSkip;
        }
        set
        {
            this.ChangeField(ref _rowsToSkip, value, t => t.RowsToSkip);
        }
    }

    [Export(nameof(RowsToTrim))]
    public int RowsToTrim
    {
        get
        {
            return _rowsToTrim;
        }
        set
        {
            this.ChangeField(ref _rowsToTrim, value, t => t.RowsToTrim);
        }
    }

    [Export(nameof(Worksheet))]
    public int Worksheet
    {
        get
        {
            return _worksheet;
        }
        set
        {
            this.ChangeField(ref _worksheet, value, t => t.Worksheet);
        }
    }

    [Export(nameof(Filename))]
    public string? Filename
    {
        get
        {
            return _filename;
        }
        set
        {
            if (this.ChangeStringField(ref _filename, value, t => t.Filename))
            {
                RowsToSkip = 0;
                RowsToTrim = 0;
                foreach (var column in Columns)
                {
                    column.SelectedIndex = -1;
                }
            }
        }
    }

    // TODO: Custom collection in order to aggregate properties and enable lookup by column purpose?
    [Export(nameof(Columns))]
    public NSMutableArray<ColumnInfo> Columns { get; } = new();

    public ColumnInfo GetColumn(string purpose)
    {
        return Columns.AsEnumerable().FirstOrDefault(column => string.Equals(column.Purpose, purpose, StringComparison.InvariantCultureIgnoreCase), ColumnInfo.Empty);
    }

    public int? GetColumnIndex(string purpose)
    {
        var info = GetColumn(purpose);
        return info.SelectedIndex >= 0 ? info.SelectedIndex : null;
    }

    public TextRecordReader? GetReader(NSComboBox? worksheetDropDown = null)
    {
        if (string.IsNullOrEmpty(Filename))
        {
            return null;
        }

        // TODO: Data bind for the worksheetDropDown instead and have the TextRecordReader support a list of worksheet names
        worksheetDropDown?.RemoveAll();

        switch (Path.GetExtension(Filename).ToLowerInvariant())
        {
            case ".csv":
                var csvReader = new CsvReader();
                try
                {
                    csvReader.Open(Filename);
                    // TODO: Data bind visibility for the WorksheetDropDown instead based upon an empty collection of items
                    if (worksheetDropDown is not null)
                    {
                        worksheetDropDown.Hidden = true;
                    }
                    return csvReader;
                }
                catch
                {
                    csvReader.Dispose();
                    throw;
                }

            case ".xlsx":
                var xlsxReader = new XlsxReader();
                try
                {
                    xlsxReader.Open(Filename, new XlsxReaderOptions
                    {
                        RowsToSkip = RowsToSkip,
                        RowsToTrim = RowsToTrim,
                        Worksheet = Worksheet >= 0 ? Worksheet : FieldInfo.Empty
                    });
                    if (worksheetDropDown is not null)
                    {
                        worksheetDropDown.Hidden = false;
                        if (xlsxReader.Document is not null)
                        {
                            foreach (var worksheet in xlsxReader.Document.Workbook.Worksheets)
                            {
                                worksheetDropDown.Add(new NSString(worksheet.Name));
                            }
                        }
                        worksheetDropDown.SelectItem(xlsxReader.CurrentWorksheetIndex);
                    }
                    return xlsxReader;
                }
                catch
                {
                    xlsxReader.Dispose();
                    throw;
                }

            default:
                throw new InvalidOperationException($"Unsupported File Type: {Path.GetExtension(Filename)}");
        }
    }
}
