// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Controls;
using Poplar.ControlsLookup;
using Poplar.ViewModels.Pages.OpSystem;

namespace Poplar.Views.Pages.OpSystem;

[GalleryPage("System file picker.", SymbolRegular.DocumentAdd24)]
public partial class FilePickerPage : INavigableView<FilePickerViewModel>
{
    public FilePickerViewModel ViewModel { get; }

    public FilePickerPage(FilePickerViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
