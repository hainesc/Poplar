using System.Windows.Controls;
using Wpf.Ui.Controls;
using Poplar.ViewModels.Pages.Manufacturing;

namespace Poplar.Views.Pages.Manufacturing;

public partial class DevicesPage : INavigableView<DevicesViewModel>
{
    public DevicesViewModel ViewModel { get; }

    public DevicesPage(DevicesViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
        
        Loaded += async (s, e) => await ViewModel.InitializeAsync();
    }
}
