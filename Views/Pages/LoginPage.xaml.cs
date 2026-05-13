using Wpf.Ui.Controls;
using Poplar.ViewModels.Pages;

namespace Poplar.Views.Pages;

public partial class LoginPage : INavigableView<LoginViewModel>
{
    public LoginViewModel ViewModel { get; }

    public LoginPage(LoginViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
