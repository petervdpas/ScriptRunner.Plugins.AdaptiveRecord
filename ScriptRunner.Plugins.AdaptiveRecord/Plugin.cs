using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ScriptRunner.Plugins.AdaptiveRecord.Interfaces;
using ScriptRunner.Plugins.Attributes;
using ScriptRunner.Plugins.Interfaces;
using ScriptRunner.Plugins.Models;
using ScriptRunner.Plugins.Utilities;

namespace ScriptRunner.Plugins.AdaptiveRecord;

/// <summary>
///     A plugin that registers and provides ...
/// </summary>
/// <remarks>
///     This plugin demonstrates how to ...
/// </remarks>
[PluginMetadata(
    "Adaptive Record",
    "A plugin that provides...",
    "Peter van de Pas",
    "1.0.0",
    PluginSystemConstants.CurrentPluginSystemVersion,
    PluginSystemConstants.CurrentFrameworkVersion,
    services: ["IAdaptiveRecord", "IAdaptiveRecordDialogService"])]
public class Plugin : BaseAsyncServicePlugin
{
    /// <summary>
    ///     Gets the name of the plugin.
    /// </summary>
    public override string Name => "Adaptive Record";
    
    /// <summary>
    /// Asynchronously initializes the plugin using the provided configuration settings.
    /// </summary>
    /// <param name="configuration">A dictionary containing configuration key-value pairs for the plugin.</param>
    /// <remarks>
    /// This method can be used to perform any initial setup required by the plugin,
    /// such as loading configuration settings or validating input.
    /// </remarks>
    public override async Task InitializeAsync(IEnumerable<PluginSettingDefinition> configuration)
    {
        // Store settings into LocalStorage
        PluginSettingsHelper.StoreSettings(configuration);

        // Optionally display the settings
        PluginSettingsHelper.DisplayStoredSettings();
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously registers the plugin's services into the application's dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <remarks>
    /// This method ensures that the `IOrmDelight` service is available for dependency injection,
    /// enabling its use throughout the application.
    /// </remarks>
    public override async Task RegisterServicesAsync(IServiceCollection services)
    {
        // Simulate async service registration (e.g., initializing an external resource)
        await Task.Delay(50);
        
        services.AddSingleton<IAdaptiveRecord, AdaptiveRecord>();
        
        services.AddSingleton<IAdaptiveRecordDialogService>(sp =>
            new AdaptiveRecordDialogService(sp.GetRequiredService<IAvaloniaControlFactory>()));
    }
    
    /// <summary>
    /// Asynchronously executes the plugin's main functionality.
    /// </summary>
    /// <remarks>
    /// This method serves as the entry point for executing the plugin's core logic.
    /// It can be used to trigger any required operations, handle tasks, or interact with external systems.
    /// </remarks>
    public override async Task ExecuteAsync()
    {
        // Example execution logic
        await Task.Delay(50);
        
        var storedSetting = PluginSettingsHelper.RetrieveSetting<string>("PluginName");
        Console.WriteLine($"Retrieved PluginName: {storedSetting}");
    }
}