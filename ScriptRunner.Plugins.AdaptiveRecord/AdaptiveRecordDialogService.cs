using System;
using System.Threading.Tasks;
using ScriptRunner.Plugins.AdaptiveRecord.Dialogs;
using ScriptRunner.Plugins.AdaptiveRecord.Interfaces;
using ScriptRunner.Plugins.AdaptiveRecord.ViewModels;
using ScriptRunner.Plugins.Interfaces;
using ScriptRunner.Plugins.Utilities;

namespace ScriptRunner.Plugins.AdaptiveRecord;

/// <summary>
/// Provides services for displaying and interacting with adaptive record dialogs in an Avalonia-based application.
/// </summary>
public class AdaptiveRecordDialogService : IAdaptiveRecordDialogService
{
    private readonly IAvaloniaControlFactory _controlFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdaptiveRecordDialogService"/> class.
    /// </summary>
    /// <param name="controlFactory">
    /// An instance of <see cref="IAvaloniaControlFactory"/> used to dynamically generate Avalonia controls
    /// for adaptive record interactions.
    /// </param>
    public AdaptiveRecordDialogService(IAvaloniaControlFactory controlFactory)
    {
        _controlFactory = controlFactory ?? throw new ArgumentNullException(nameof(controlFactory));
    }
    
    /// <summary>
    /// Displays a modal dialog for selecting or interacting with adaptive records.
    /// </summary>
    /// <param name="adaptiveRecord">
    /// An instance of <see cref="IAdaptiveRecord"/> that manages the dynamic data and structure
    /// of the records displayed in the dialog. This interface provides access to the data source
    /// and allows interactions such as selecting or updating records.
    /// </param>
    /// <param name="title">
    /// The title of the dialog window. Defaults to "Select a Record".
    /// </param>
    /// <param name="width">
    /// The width of the dialog window in pixels. Defaults to 1280.
    /// </param>
    /// <param name="height">
    /// The height of the dialog window in pixels. Defaults to 720.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. 
    /// The task result is a <see cref="string"/> containing the identifier of the selected record 
    /// if a selection is made, or <c>null</c> if the dialog is closed without a selection.
    /// </returns>
    /// <remarks>
    /// This method initializes and displays an <see cref="AdaptiveRecordDialog"/> configured to show
    /// a list of records using the <paramref name="adaptiveRecord"/> instance. The dialog's <c>DataContext</c> 
    /// is set to an <see cref="AdaptiveRecordModel"/>, which manages the records and user interactions.
    /// </remarks>
    public async Task<string?> GetAdaptiveRecordAsync(
        IAdaptiveRecord adaptiveRecord, 
        string title = "Select a Record", 
        int width = 1280,
        int height = 720)
    {
        var dialog = new AdaptiveRecordDialog
        {
            Title = title,
            Width = width,
            Height = height
        };

        var viewModel = new AdaptiveRecordModel(dialog, adaptiveRecord, _controlFactory);
        dialog.DataContext = viewModel;

        return await DialogHelper.ShowDialogAsync(dialog.ShowDialog<string?>);
    }
}