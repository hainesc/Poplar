// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Lepo.i18n.DependencyInjection;
using Wpf.Ui.DependencyInjection;
using Poplar.DependencyModel;
using Poplar.Resources;
using Poplar.Services;
using Poplar.Services.Contracts;
using Poplar.ViewModels.Pages;
using Poplar.ViewModels.Windows;
using Poplar.Views.Pages;
using Poplar.Views.Windows;

namespace Poplar;

public partial class App
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    private static readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(c =>
        {
            _ = c.SetBasePath(AppContext.BaseDirectory);
        })
        .ConfigureServices(
            (_1, services) =>
            {
                _ = services.AddNavigationViewPageProvider();

                // App Host
                _ = services.AddHostedService<ApplicationHostService>();

                // Main window container with navigation
                _ = services.AddSingleton<IWindow, MainWindow>();
                _ = services.AddSingleton<MainWindowViewModel>();
                _ = services.AddSingleton<INavigationService, NavigationService>();
                _ = services.AddSingleton<ISnackbarService, SnackbarService>();
                _ = services.AddSingleton<IContentDialogService, ContentDialogService>();
                _ = services.AddSingleton<WindowsProviderService>();
                _ = services.AddSingleton<SettingsService>();

                // Backend & Auth
                _ = services.AddSingleton<BackendService>();
                _ = services.AddSingleton<ProductService>();
                _ = services.AddSingleton<SessionManager>();
                _ = services.AddSingleton<DeviceService>();

                // Top-level pages
                _ = services.AddSingleton<LoginPage>();
                _ = services.AddSingleton<LoginViewModel>();
                _ = services.AddSingleton<DashboardPage>();
                _ = services.AddSingleton<DashboardViewModel>();
                _ = services.AddSingleton<AllControlsPage>();
                _ = services.AddSingleton<AllControlsViewModel>();
                _ = services.AddSingleton<SettingsPage>();
                _ = services.AddSingleton<SettingsViewModel>();
                _ = services.AddSingleton<Poplar.Views.Pages.Manufacturing.DevicesPage>();
                _ = services.AddSingleton<Poplar.ViewModels.Pages.Manufacturing.DevicesViewModel>();
                _ = services.AddSingleton<Poplar.Views.Pages.Workspaces.WorkspacesPage>();
                _ = services.AddSingleton<Poplar.ViewModels.Pages.Workspaces.WorkspacesViewModel>();
                _ = services.AddSingleton<Poplar.Views.Pages.Manufacturing.ProductsPage>();
                _ = services.AddSingleton<Poplar.ViewModels.Pages.Manufacturing.ProductsViewModel>();

                // All other pages and view models
                _ = services.AddTransientFromNamespace("Poplar.Views", GalleryAssembly.Asssembly);
                _ = services.AddTransientFromNamespace(
                    "Poplar.ViewModels",
                    GalleryAssembly.Asssembly
                );

                _ = services.AddStringLocalizer(b =>
                {
                    b.FromResource<Translations>(new("en-US"));
                    b.FromResource<Translations>(new("pl-PL"));
                    b.FromResource<Translations>(new("zh-CN"));
                });
            }
        )
        .Build();

    /// <summary>
    /// Gets registered service.
    /// </summary>
    /// <typeparam name="T">Type of the service to get.</typeparam>
    /// <returns>Instance of the service or <see langword="null"/>.</returns>
    public static T GetRequiredService<T>()
        where T : class
    {
        return _host.Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Occurs when the application is loading.
    /// </summary>
    private void OnStartup(object sender, StartupEventArgs e)
    {
        var settingsService = _host.Services.GetRequiredService<SettingsService>();
        var culture = new System.Globalization.CultureInfo(settingsService.Settings.Language);
        System.Threading.Thread.CurrentThread.CurrentCulture = culture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

        _host.Start();
    }

    /// <summary>
    /// Occurs when the application is closing.
    /// </summary>
    private void OnExit(object sender, ExitEventArgs e)
    {
        _host.StopAsync().Wait();

        _host.Dispose();
    }

    /// <summary>
    /// Occurs when an exception is thrown by an application but not handled.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
    }
}
