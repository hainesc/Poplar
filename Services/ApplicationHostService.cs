// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Poplar.Services.Contracts;
using Poplar.Views.Pages;
using Poplar.Views.Windows;

namespace Poplar.Services;

/// <summary>
/// Managed host of the application.
/// </summary>
public class ApplicationHostService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public ApplicationHostService(IServiceProvider serviceProvider)
    {
        // If you want, you can do something with these services at the beginning of loading the application.
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return HandleActivationAsync();
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates main window during activation.
    /// </summary>
    private Task HandleActivationAsync()
    {
        if (Application.Current.Windows.OfType<MainWindow>().Any())
        {
            return Task.CompletedTask;
        }

        IWindow mainWindow = _serviceProvider.GetRequiredService<IWindow>();
        mainWindow.Loaded += OnMainWindowLoaded;
        mainWindow?.Show();

        return Task.CompletedTask;
    }

    private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow mainWindow)
        {
            return;
        }

        var session = _serviceProvider.GetRequiredService<SessionManager>();

        try
        {
            // Just trigger the restore; MainWindow listens to SessionManager.PropertyChanged
            // and will update its own UI/Navigation accordingly.
            _ = await session.TryRestoreSessionAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Session restore failed: {ex.Message}");
        }
    }
}
