// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Controls;
using Poplar.ControlsLookup;
using Poplar.ViewModels.Pages.OpSystem;

namespace Poplar.Views.Pages.OpSystem;

[GalleryPage("System clipboard.", SymbolRegular.Desktop24)]
public partial class ClipboardPage : INavigableView<ClipboardViewModel>
{
    public ClipboardViewModel ViewModel { get; }

    public ClipboardPage(ClipboardViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
