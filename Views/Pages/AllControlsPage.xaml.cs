// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Poplar.ViewModels.Pages;

namespace Poplar.Views.Pages;

public partial class AllControlsPage : INavigableView<AllControlsViewModel>
{
    public AllControlsViewModel ViewModel { get; }

    public AllControlsPage(AllControlsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
