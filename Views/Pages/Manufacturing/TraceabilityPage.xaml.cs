using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using Poplar.ViewModels.Pages.Manufacturing;

namespace Poplar.Views.Pages.Manufacturing;

/// <summary>
/// Interaction logic for TraceabilityPage.xaml
/// </summary>
public partial class TraceabilityPage : INavigationAware
{
    public TraceabilityPageViewModel ViewModel { get; }

    public TraceabilityPage(TraceabilityPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public async Task OnNavigatedToAsync()
    {
        await ViewModel.InitializeAsync();
        await ViewModel.LoadRecordsAsync();
    }

    public Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }
}
