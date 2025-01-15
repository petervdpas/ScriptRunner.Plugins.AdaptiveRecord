using System.Threading.Tasks;

namespace ScriptRunner.Plugins.AdaptiveRecord.Interfaces;

/// <summary>
///     Provides functionality for displaying and interacting with adaptive records in a dialog.
/// </summary>
public interface IAdaptiveRecordDialogService
{
    /// <summary>
    ///     Displays a modal dialog for selecting or interacting with adaptive records.
    /// </summary>
    /// <param name="adaptiveRecord">
    ///     An instance of <see cref="IAdaptiveRecord" /> that manages the dynamic data and structure
    ///     of the records displayed in the dialog. This interface provides access to the data source
    ///     and allows interactions such as selecting or updating records.
    /// </param>
    /// <param name="title">
    ///     The title of the dialog window. Defaults to "Select a Record".
    /// </param>
    /// <param name="width">
    ///     The width of the dialog window in pixels. Defaults to 1280.
    /// </param>
    /// <param name="height">
    ///     The height of the dialog window in pixels. Defaults to 720.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation.
    ///     The task result is a <see cref="string" /> containing the identifier of the selected record
    ///     if a selection is made, or <c>null</c> if the dialog is closed without a selection.
    /// </returns>
    /// <remarks>
    ///     This method is used to initialize and display a modal dialog window that allows users
    ///     to interact with adaptive records. The dialog is centered on the main application window
    ///     and operates asynchronously, returning the result of the user's interaction once the dialog is closed.
    /// </remarks>
    Task<string?> GetAdaptiveRecordAsync(
        IAdaptiveRecord adaptiveRecord,
        string title = "Select a Record",
        int width = 1280,
        int height = 720);
}