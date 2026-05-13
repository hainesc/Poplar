using Poplar.ViewModels.Pages.Manufacturing;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Poplar.Views.Pages.Manufacturing;

public partial class ProductsPage : INavigationAware
{
    public ProductsViewModel ViewModel { get; }

    public ProductsPage(ProductsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public async Task OnNavigatedToAsync()
    {
        await ViewModel.InitializeAsync();
    }

    public Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }
}
