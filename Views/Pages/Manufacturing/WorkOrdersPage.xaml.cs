using System.Windows.Controls;
using System.Threading.Tasks;
using Wpf.Ui.Controls;
using Poplar.ViewModels.Pages.Manufacturing;

namespace Poplar.Views.Pages.Manufacturing;

public partial class WorkOrdersPage : INavigationAware
{
    public WorkOrdersPage(WorkOrdersViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public WorkOrdersViewModel ViewModel { get; }

    public async Task OnNavigatedToAsync()
    {
        await ViewModel.InitializeAsync();
    }

    public Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }
}
