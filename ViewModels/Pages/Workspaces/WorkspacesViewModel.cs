using Poplar.Services;
using System.Diagnostics;

namespace Poplar.ViewModels.Pages.Workspaces;

public partial class WorkspacesViewModel : ObservableObject
{
    private readonly SessionManager _session;
    private readonly BackendService _backend;

    [ObservableProperty]
    private string _workspaceName = string.Empty;

    [ObservableProperty]
    private string _workspaceMode = "discrete"; // discrete, process

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public WorkspacesViewModel(SessionManager session, BackendService backend)
    {
        _session = session;
        _backend = backend;
    }

    [RelayCommand]
    private async Task CreateWorkspaceAsync()
    {
        if (string.IsNullOrWhiteSpace(WorkspaceName))
        {
            ErrorMessage = "Please enter a workspace name.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (!_backend.IsInitialized) await _backend.InitializeAsync();

            var response = await _backend.RbacVm.CreateWorkspace(WorkspaceName, WorkspaceMode);
            
            // The response contains a new token with the workspace_id included
            // We update the session with the new token
            _session.UpdateToken(response.token);
            
            Debug.WriteLine($"[Workspaces] Created workspace '{WorkspaceName}' (ID: {response.workspace.id})");
            
            // Navigation will be handled by MainWindow reacting to SessionManager.WorkspaceId change
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"Failed to create workspace: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
