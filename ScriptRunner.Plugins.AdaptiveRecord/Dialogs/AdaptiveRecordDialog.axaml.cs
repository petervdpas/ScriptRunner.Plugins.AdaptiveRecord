using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ScriptRunner.Plugins.AdaptiveRecord.ViewModels;

namespace ScriptRunner.Plugins.AdaptiveRecord.Dialogs;

/// <summary>
/// Represents the dialog for displaying and managing adaptive records.
/// </summary>
public partial class AdaptiveRecordDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdaptiveRecordDialog"/> class.
    /// </summary>
    public AdaptiveRecordDialog()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}