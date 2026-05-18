// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Extensions.Localization;
using Wpf.Ui.Controls;
using Poplar.Services;
using Poplar.Resources;
using Poplar.Views.Pages;
using Poplar.Views.Pages;
using CommunityToolkit.Mvvm.Messaging;
using Poplar.Models;

namespace Poplar.ViewModels.Windows;

public partial class MainWindowViewModel : ViewModel
{
    private readonly IStringLocalizer<Translations> _localizer;
    public SessionManager Session { get; }

    public MainWindowViewModel(IStringLocalizer<Translations> localizer, SessionManager session)
    {
        _localizer = localizer;
        Session = session;
        _applicationTitle = _localizer["WPF UI Gallery"];

        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (r, m) =>
        {
            ApplicationTitle = _localizer["WPF UI Gallery"];
        });
    }

    [ObservableProperty]
    private string _applicationTitle;

    [ObservableProperty]
    private ObservableCollection<object> _menuItems =
    [
        new NavigationViewItem("Products", SymbolRegular.Box24, typeof(Poplar.Views.Pages.Manufacturing.ProductsPage)),
        new NavigationViewItem("Devices", SymbolRegular.SpeakerSettings24, typeof(Poplar.Views.Pages.Manufacturing.DevicesPage)),
        new NavigationViewItem("Work Orders", SymbolRegular.Clipboard24, typeof(Poplar.Views.Pages.Manufacturing.WorkOrdersPage)),
        new NavigationViewItem("Runtime", SymbolRegular.Play24, typeof(Poplar.Views.Pages.Manufacturing.RuntimePage)),
    ];

    [ObservableProperty]
    private ObservableCollection<object> _footerMenuItems =
    [
        new NavigationViewItem("Settings", SymbolRegular.Settings24, typeof(SettingsPage)),
    ];

    [ObservableProperty]
    private ObservableCollection<Control> _trayMenuItems =
    [
        new Wpf.Ui.Controls.MenuItem()
        {
            Header = "Home",
            Tag = "tray_home",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
        },
        new Wpf.Ui.Controls.MenuItem()
        {
            Header = "Settings",
            Tag = "tray_settings",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
        },
        new Separator(),
        new Wpf.Ui.Controls.MenuItem()
        {
            Header = "Close",
            Tag = "tray_close",
        },
    ];

    [RelayCommand]
    private async Task OnLogout()
    {
        await Session.LogoutAsync();
    }
}
