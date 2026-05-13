// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.ComponentModel;
using Wpf.Ui.Controls;
using Poplar.Services;
using Poplar.Services.Contracts;
using Poplar.ViewModels.Windows;
using Poplar.Views.Pages;

namespace Poplar.Views.Windows;

public partial class MainWindow : IWindow
{
    private readonly SessionManager _session;
    private readonly IServiceProvider _serviceProvider;
    private bool _isInitialized = false;

    public MainWindow(
        MainWindowViewModel viewModel,
        INavigationService navigationService,
        IServiceProvider serviceProvider,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService
    )
    {
        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);

        ViewModel = viewModel;
        _session = serviceProvider.GetRequiredService<SessionManager>();
        _serviceProvider = serviceProvider;
        DataContext = this;

        InitializeComponent();

        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        navigationService.SetNavigationControl(NavigationView);
        contentDialogService.SetDialogHost(RootContentDialog);
        SetupTrayMenuEvents();

        _isInitialized = true;

        // React to authentication state changes
        _session.PropertyChanged += OnSessionPropertyChanged;

        // Sync initial state after the window is fully loaded to avoid binding/null issues
        Dispatcher.BeginInvoke(() =>
        {
            UpdateNavigationForAuthState(_session.IsAuthenticated, _session.WorkspaceId);
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    public MainWindowViewModel ViewModel { get; }

    private bool _isUserClosedPane;

    private bool _isPaneOpenedOrClosedFromCode;

    private void SetupTrayMenuEvents()
    {
        foreach (var menuItem in ViewModel.TrayMenuItems)
        {
            if (menuItem is MenuItem item)
            {
                item.Click += OnTrayMenuItemClick;
            }
        }
    }

    private void OnTrayMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.MenuItem menuItem)
        {
            return;
        }

        var tag = menuItem.Tag?.ToString() ?? string.Empty;

        Debug.WriteLine($"System Tray Click: {menuItem.Header}, Tag: {tag}");

        switch (tag)
        {
            case "tray_home":
                HandleTrayHomeClick();
                break;
            case "tray_settings":
                HandleTraySettingsClick();
                break;
            case "tray_close":
                HandleTrayCloseClick();
                break;
            default:
                if (!string.IsNullOrEmpty(tag))
                {
                    System.Diagnostics.Debug.WriteLine($"unknown Tag: {tag}");
                }

                break;
        }
    }

    private void HandleTrayHomeClick()
    {
        System.Diagnostics.Debug.WriteLine("Tray menu - Home Click");

        ShowAndActivateWindow();

        NavigateToPage(typeof(DashboardPage));
    }

    private void HandleTraySettingsClick()
    {
        System.Diagnostics.Debug.WriteLine("Tray menu - Settings Click");

        ShowAndActivateWindow();

        NavigateToPage(typeof(SettingsPage));
    }

    private static void HandleTrayCloseClick()
    {
        System.Diagnostics.Debug.WriteLine("Tray menu - Close Click");

        Application.Current.Shutdown();
    }

    private void ShowAndActivateWindow()
    {
        if (WindowState == WindowState.Minimized)
        {
            SetCurrentValue(WindowStateProperty, WindowState.Normal);
        }

        Show();
        _ = Activate();
        _ = Focus();
    }

    private void NavigateToPage(Type pageType)
    {
        if (NavigationView == null || NavigationView.Visibility != Visibility.Visible)
        {
            return;
        }

        try
        {
            NavigationView.Navigate(pageType);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NavigateToPage {pageType.Name} Error: {ex.Message}");
        }
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SessionManager.IsAuthenticated) || 
            e.PropertyName == nameof(SessionManager.WorkspaceId))
        {
            Dispatcher.Invoke(() =>
            {
                UpdateNavigationForAuthState(_session.IsAuthenticated, _session.WorkspaceId);
            });
        }
    }

    private void UpdateNavigationForAuthState(bool isAuthenticated, int workspaceId)
    {
        if (!_isInitialized || NavigationView == null || AuthFrame == null)
        {
            return;
        }

        if (isAuthenticated)
        {
            if (workspaceId > 0)
            {
                // Full access
                NavigationView.Visibility = Visibility.Visible;
                AuthFrame.Visibility = Visibility.Collapsed;
                AuthFrame.Content = null;

                NavigateToPage(typeof(DashboardPage));
            }
            else
            {
                // Authenticated but no workspace - show workspace setup
                NavigationView.Visibility = Visibility.Collapsed;
                AuthFrame.Visibility = Visibility.Visible;
                
                if (AuthFrame.Content?.GetType() != typeof(Poplar.Views.Pages.Workspaces.WorkspacesPage))
                {
                    AuthFrame.Navigate(_serviceProvider.GetRequiredService<Poplar.Views.Pages.Workspaces.WorkspacesPage>());
                }
            }
        }
        else
        {
            // Not authenticated - show login
            NavigationView.Visibility = Visibility.Collapsed;
            AuthFrame.Visibility = Visibility.Visible;

            if (AuthFrame.Content?.GetType() != typeof(LoginPage))
            {
                AuthFrame.Navigate(_serviceProvider.GetRequiredService<LoginPage>());
            }
        }
    }

    private void OnNavigationSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.NavigationView navigationView)
        {
            return;
        }

        var targetPage = navigationView.SelectedItem?.TargetPageType;
        var hideHeader = targetPage == typeof(DashboardPage) || targetPage == typeof(LoginPage);

        NavigationView.SetCurrentValue(
            NavigationView.HeaderVisibilityProperty,
            hideHeader ? Visibility.Collapsed : Visibility.Visible
        );
    }

    private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isUserClosedPane)
        {
            return;
        }

        _isPaneOpenedOrClosedFromCode = true;
        NavigationView.SetCurrentValue(NavigationView.IsPaneOpenProperty, e.NewSize.Width > 1200);
        _isPaneOpenedOrClosedFromCode = false;
    }

    private void NavigationView_OnPaneOpened(NavigationView sender, RoutedEventArgs args)
    {
        if (_isPaneOpenedOrClosedFromCode)
        {
            return;
        }

        _isUserClosedPane = false;
    }

    private void NavigationView_OnPaneClosed(NavigationView sender, RoutedEventArgs args)
    {
        if (_isPaneOpenedOrClosedFromCode)
        {
            return;
        }

        _isUserClosedPane = true;
    }
    private void UserAvatar_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.ContextMenu != null)
        {
            element.ContextMenu.PlacementTarget = element;
            element.ContextMenu.IsOpen = true;
            e.Handled = true;
        }
    }
}
