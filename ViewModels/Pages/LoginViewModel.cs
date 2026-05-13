using Poplar.Services;

namespace Poplar.ViewModels.Pages;

public sealed partial class LoginViewModel : ViewModel
{
    private readonly SessionManager _session;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isRegistering;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public LoginViewModel(SessionManager session, INavigationService navigation)
    {
        _session = session;
        _navigation = navigation;
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter username and password.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (IsRegistering)
            {
                await _session.RegisterAsync(Username, Password);
            }
            else
            {
                await _session.LoginAsync(Username, Password);
            }

            // Navigation is handled by MainWindow reacting to SessionManager.IsAuthenticated
        }
        catch (Exception ex)
        {
            ErrorMessage = IsRegistering
                ? $"Registration failed: {ex.Message}"
                : $"Login failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleMode()
    {
        IsRegistering = !IsRegistering;
        ErrorMessage = null;
    }
}
