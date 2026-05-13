using System.Windows.Controls;
using Wpf.Ui.Controls;
using Poplar.ViewModels.Pages.Workspaces;

namespace Poplar.Views.Pages.Workspaces;

public partial class WorkspacesPage : INavigableView<WorkspacesViewModel>
{
    public WorkspacesViewModel ViewModel { get; }

    public WorkspacesPage(WorkspacesViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
