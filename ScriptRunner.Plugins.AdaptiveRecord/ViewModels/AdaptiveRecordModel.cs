using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using ScriptRunner.Plugins.AdaptiveRecord.Interfaces;
using ScriptRunner.Plugins.Interfaces;
using ScriptRunner.Plugins.Models;

namespace ScriptRunner.Plugins.AdaptiveRecord.ViewModels;

/// <summary>
/// ViewModel for managing an adaptive record-based UI. This model facilitates operations such as adding, deleting, 
/// saving, and updating records dynamically, as well as generating UI controls based on data structures.
/// </summary>
public class AdaptiveRecordModel : ReactiveObject
{
    private readonly IAvaloniaControlFactory _controlFactory;
    private readonly Window _dialog;
    private readonly IAdaptiveRecord _adaptiveRecord;

    private ObservableCollection<RecordItem> _items;
    private string? _recordIdentifier;
    private RecordItem? _selectedItem;

    private string _statusMessage = "Ready";

    /// <summary>
    /// Initializes a new instance of the <see cref="AdaptiveRecordModel"/> class.
    /// </summary>
    /// <param name="dialog">The parent dialog window managing this ViewModel.</param>
    /// <param name="adaptiveRecord">The adaptive record interface handling dynamic data and structure.</param>
    /// <param name="controlFactory">The factory for creating Avalonia controls dynamically based on data.</param>
    public AdaptiveRecordModel(Window dialog, IAdaptiveRecord adaptiveRecord, IAvaloniaControlFactory controlFactory)
    {
        _controlFactory = controlFactory ?? throw new ArgumentNullException(nameof(controlFactory));
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
        _adaptiveRecord = adaptiveRecord ?? throw new ArgumentNullException(nameof(adaptiveRecord));

        _items = adaptiveRecord.DynamicType != null
            ? new ObservableCollection<RecordItem>(
                adaptiveRecord.GetDataTable().AsEnumerable()
                    .Select(row => new RecordItem(row, adaptiveRecord.DynamicType))
            )
            : [];

        SelectedItem = _items.FirstOrDefault();

        // Command initializations
        QuitCommand = ReactiveCommand.Create(CloseDialog);
        AddRecordCommand = ReactiveCommand.Create(AddRecord);
        DeleteRecordCommand = ReactiveCommand.Create(DeleteRecord);
        SaveRecordCommand = ReactiveCommand.Create(SaveRecord);
        SaveChangesCommand = ReactiveCommand.Create(SaveChangedRecords);
    }

    /// <summary>
    /// Command to quit the dialog and close it without saving changes.
    /// </summary>
    public ReactiveCommand<Unit, Unit> QuitCommand { get; }

    /// <summary>
    /// Command to add a new record.
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddRecordCommand { get; }

    /// <summary>
    /// Command to delete the currently selected record.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeleteRecordCommand { get; }

    /// <summary>
    /// Command to save changes to the currently selected record.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveRecordCommand { get; }

    /// <summary>
    /// Command to save all modified records.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveChangesCommand { get; }

    /// <summary>
    /// Gets or sets the collection of record items being displayed.
    /// </summary>
    public ObservableCollection<RecordItem> Items
    {
        get => _items;
        set => this.RaiseAndSetIfChanged(ref _items, value);
    }

    /// <summary>
    /// Gets or sets the currently selected record item.
    /// Updates UI controls when the selection changes.
    /// </summary>
    public RecordItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedItem, value);
            // Trigger updates in the UI based on the selected item
            this.RaisePropertyChanged(nameof(DetailControls));

            RecordIdentifier = $"Record on: {_selectedItem?.DisplayName}";
        }
    }

    /// <summary>
    /// Gets or sets a display identifier for the currently selected record.
    /// </summary>
    public string? RecordIdentifier
    {
        get => _recordIdentifier;
        set => this.RaiseAndSetIfChanged(ref _recordIdentifier, value);
    }

    /// <summary>
    /// Gets the UI controls dynamically generated for the currently selected record.
    /// </summary>
    public IEnumerable<Control> DetailControls =>
        SelectedItem != null
            ? _controlFactory.GenerateControls(
                _adaptiveRecord.DynamicType, SelectedItem)
            : [];

    /// <summary>
    /// Gets or sets the status message displayed in the UI.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    /// <summary>
    /// Closes the dialog window without returning any result.
    /// </summary>
    private void CloseDialog()
    {
        _dialog.Close(null);
    }

    /// <summary>
    /// Adds a new record to the collection and the underlying data source.
    /// </summary>
    private void AddRecord()
    {
        if (_adaptiveRecord.DynamicType == null) return;

        var newInstance = Activator.CreateInstance(_adaptiveRecord.DynamicType);
        if (newInstance == null) return;

        _adaptiveRecord.AddDataRow(newInstance); // This will trigger the AddRow callback

        var id = (long)(_adaptiveRecord.DynamicType?.GetProperty("Id")?.GetValue(newInstance) ?? 0);

        var newRecordItem = new RecordItem(
            _adaptiveRecord.GetDataRowById(id),
            _adaptiveRecord.DynamicType!);
        newRecordItem.MarkAsDirty();

        _items.Add(newRecordItem);
        SelectedItem = newRecordItem;

        SetStatusMessage($"Record with ID:{id} was added successfully.");
    }

    /// <summary>
    /// Saves changes to the currently selected record.
    /// </summary>
    private void SaveRecord()
    {
        if (SelectedItem == null || _adaptiveRecord.DynamicType == null) return;

        var currentSelectedItemId = SelectedItem.DataRow["Id"];

        SaveRecordInternal(SelectedItem);
        SelectedItem.MarkAsClean();

        RefreshItems(currentSelectedItemId);

        SetStatusMessage($"Record with ID:{currentSelectedItemId} was saved successfully.");
    }

    /// <summary>
    /// Saves all records that have been modified.
    /// </summary>
    private void SaveChangedRecords()
    {
        if (SelectedItem == null || _adaptiveRecord.DynamicType == null) return;

        var currentSelectedItemId = SelectedItem.DataRow["Id"];

        var changedItems = Items.Where(item => item.IsDirty).ToList();
        foreach (var item in changedItems)
        {
            //Log.Debug($"{item.DisplayName} has been modified.");
            SaveRecordInternal(item);
            item.MarkAsClean();
        }

        RefreshItems(currentSelectedItemId);

        SetStatusMessage($"{changedItems.Count} records saved successfully.");
    }

    /// <summary>
    /// Internal helper to save a specific record item to the data source.
    /// </summary>
    /// <param name="recordItem">The record item to save.</param>
    private void SaveRecordInternal(RecordItem recordItem)
    {
        if (_adaptiveRecord.DynamicType == null) return;

        var dataRow = recordItem.DataRow;
        var instance = Activator.CreateInstance(_adaptiveRecord.DynamicType);
        if (instance == null) return;

        foreach (var prop in _adaptiveRecord.DynamicType.GetProperties())
        {
            if (!dataRow.Table.Columns.Contains(prop.Name)) continue;
            var value = dataRow[prop.Name];
            if (value == DBNull.Value) continue;

            if (prop.PropertyType == typeof(DateTime) && value is string dateString &&
                DateTime.TryParse(dateString, out var parsedDateTime))
                prop.SetValue(instance, parsedDateTime);
            else if (prop.PropertyType == typeof(DateTimeOffset) && value is string offsetDateString &&
                     DateTimeOffset.TryParse(offsetDateString, out var parsedDateTimeOffset))
                prop.SetValue(instance, parsedDateTimeOffset);
            else
                prop.SetValue(instance, Convert.ChangeType(value, prop.PropertyType));
        }

        _adaptiveRecord.UpdateDataRow(instance);
    }

    /// <summary>
    /// Deletes the currently selected record from the collection and the data source.
    /// </summary>
    private void DeleteRecord()
    {
        if (SelectedItem == null || _adaptiveRecord.DynamicType == null) return;

        var dataRow = SelectedItem.DataRow;
        var deletedRecordId = dataRow["Id"];

        // Create an instance of the DynamicType and populate it from the DataRow
        var instance = Activator.CreateInstance(_adaptiveRecord.DynamicType);
        if (instance == null) return;

        foreach (var prop in _adaptiveRecord.DynamicType.GetProperties())
        {
            if (!dataRow.Table.Columns.Contains(prop.Name)) continue;
            var value = dataRow[prop.Name];
            if (value != DBNull.Value) prop.SetValue(instance, Convert.ChangeType(value, prop.PropertyType));
        }

        _adaptiveRecord.DeleteDataRow(instance);

        _items.Remove(SelectedItem);

        RefreshItems();

        SelectedItem = _items.FirstOrDefault();

        SetStatusMessage($"Record with ID:{deletedRecordId} was deleted successfully.");
    }

    /// <summary>
    /// Refreshes the item collection after changes to the data source.
    /// </summary>
    /// <param name="selectedItemId">The ID of the currently selected item, if any.</param>
    private void RefreshItems(object? selectedItemId = null)
    {
        if (_adaptiveRecord.DynamicType == null) return;

        // Rebuild the collection from the updated DataTable
        _items = new ObservableCollection<RecordItem>(
            _adaptiveRecord.GetDataTable().AsEnumerable()
                .Select(row => new RecordItem(row, _adaptiveRecord.DynamicType))
        );

        // Notify the UI about the updated Items collection
        this.RaisePropertyChanged(nameof(Items));

        if (selectedItemId != null)
            // Restore selected item if it exists in the new list
            SelectedItem = _items.FirstOrDefault(item => item.DataRow["Id"].Equals(selectedItemId));
    }

    /// <summary>
    /// Updates the status message displayed in the UI.
    /// </summary>
    /// <param name="msg">The message to display.</param>
    private void SetStatusMessage(string msg)
    {
        //Log.Debug(msg);
        StatusMessage = msg;
    }
    
}