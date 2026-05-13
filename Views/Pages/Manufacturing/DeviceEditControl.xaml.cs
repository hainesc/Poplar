using System.Windows.Controls;
using Poplar.ViewModels.Pages.Manufacturing;

namespace Poplar.Views.Pages.Manufacturing;

public partial class DeviceEditControl : UserControl
{
    public DeviceEditViewModel ViewModel { get; }

    public DeviceEditControl(DeviceEditViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
