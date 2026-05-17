using System.Windows.Controls;
using System.Threading.Tasks;
using Wpf.Ui.Controls;
using Poplar.ViewModels.Pages.Manufacturing;

namespace Poplar.Views.Pages.Manufacturing;

/// <summary>
/// Interaction logic for RuntimePage.xaml
/// </summary>
public partial class RuntimePage : INavigationAware
{
    public RuntimePage(RuntimeViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public RuntimeViewModel ViewModel { get; }

    public async Task OnNavigatedToAsync()
    {
        await ViewModel.InitializeAsync();
    }

    public Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }
}
