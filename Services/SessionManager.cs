using System.Security.Cryptography;
using System.Text;
using uniffi.stump;

namespace Poplar.Services;

/// <summary>
/// Manages authentication state across the application.
/// Wraps RbacViewModel FFI calls and provides observable properties for UI binding.
/// Persists refresh token using DPAPI for auto-login on restart.
/// 
/// Token refresh uses an interceptor pattern: GetAccessTokenAsync() checks expiry
/// and transparently refreshes before returning, similar to Axios interceptors in React.
/// </summary>
public sealed partial class SessionManager : ObservableObject, IDisposable
{
    private readonly BackendService _backend;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private UserRecord? _currentUser;

    [ObservableProperty]
    private AuthToken? _currentToken;

    public SessionManager(BackendService backend)
    {
        _backend = backend;
    }

    /// <summary>
    /// Gets the current access token, refreshing transparently if expired.
    /// This is the interceptor — all callers just await this and get a valid token.
    /// Returns null if not authenticated.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        if (!IsAuthenticated || CurrentToken == null)
            return null;

        // Token still valid (with 30s buffer)
        if (DateTimeOffset.UtcNow < _tokenExpiresAt.AddSeconds(-30))
            return CurrentToken.accessToken;

        // Token expired or about to expire — refresh
        return await RefreshTokenAsync();
    }

    /// <summary>
    /// Attempts to restore a previous session using persisted refresh token.
    /// </summary>
    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            if (!_backend.IsInitialized)
            {
                await _backend.InitializeAsync();
            }

            var refreshToken = LoadPersistedRefreshToken();
            if (string.IsNullOrEmpty(refreshToken))
            {
                Debug.WriteLine("[Session] No persisted refresh token found.");
                return false;
            }

            // NOTE: Requires regenerated stump.cs containing the RefreshToken method
            // RefreshToken now returns UserRecord and internally caches the new AuthToken
            var user = await _backend.RbacVm.RefreshToken(refreshToken);
            
            if (user == null)
            {
                Debug.WriteLine("[Session] FFI RefreshToken returned null.");
                return false;
            }

            var token = await _backend.RbacVm.GetAuthToken();

            if (token == null)
            {
                Debug.WriteLine("[Session] FFI GetAuthToken returned null after successful refresh.");
                return false;
            }

            SetSession(user, token);
            PersistRefreshToken(token.refreshToken);
            
            Debug.WriteLine($"[Session] Restored session for user: {user.username}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[Session] CRITICAL: Failed to restore session: {ex.GetType().Name} - {ex.Message}");
            if (ex.InnerException != null) Debug.WriteLine($"[Session] Inner: {ex.InnerException.Message}");
            ClearPersistedData();
            ClearSession();
            return false;
        }
    }

    /// <summary>
    /// Performs login via FFI.
    /// </summary>
    public async Task LoginAsync(string username, string password)
    {
        if (!_backend.IsInitialized)
        {
            await _backend.InitializeAsync();
        }

        var user = await _backend.RbacVm.Login(username, password);
        var token = await _backend.RbacVm.GetAuthToken();

        if (token == null)
        {
            throw new InvalidOperationException("Login succeeded but no token was returned.");
        }

        SetSession(user, token);
        PersistRefreshToken(token.refreshToken);
        Debug.WriteLine($"[Session] Login successful: {user.username}");
    }

    /// <summary>
    /// Performs registration via FFI, then auto-login.
    /// Register only creates the user in DB; we must call Login to get a token.
    /// </summary>
    public async Task RegisterAsync(string username, string password)
    {
        if (!_backend.IsInitialized)
        {
            await _backend.InitializeAsync();
        }

        // Register creates user in DB but does not generate a token
        _ = await _backend.RbacVm.Register(username, password);

        // Login to get the actual token and establish session
        await LoginAsync(username, password);
        Debug.WriteLine($"[Session] Registration + login successful: {username}");
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            if (_backend.IsInitialized)
            {
                await _backend.RbacVm.Logout();
            }
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[Session] Logout FFI error (ignored): {ex.Message}");
        }

        ClearSession();
        ClearPersistedData();
        Debug.WriteLine("[Session] Logged out.");
    }

    /// <summary>
    /// Interceptor: refresh the token if expired. Thread-safe via SemaphoreSlim.
    /// </summary>
    private async Task<string?> RefreshTokenAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            // Double-check: another caller may have already refreshed
            if (DateTimeOffset.UtcNow < _tokenExpiresAt.AddSeconds(-30) && CurrentToken != null)
                return CurrentToken.accessToken;

            Debug.WriteLine("[Session] Token expired, refreshing...");

            if (CurrentToken == null) return null;

            // Use the new refresh_token FFI method (returns UserRecord)
            var user = await _backend.RbacVm.RefreshToken(CurrentToken.refreshToken);
            var token = await _backend.RbacVm.GetAuthToken();
            
            if (user == null || token == null)
            {
                Debug.WriteLine("[Session] Refresh failed, session expired.");
                Application.Current.Dispatcher.Invoke(ClearSession);
                return null;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentUser = user;
                CurrentToken = token;
                _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.expiresIn);
                PersistRefreshToken(token.refreshToken);
            });

            Debug.WriteLine("[Session] Token refreshed successfully.");
            return token.accessToken;
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[Session] Token refresh failed: {ex.Message}");
            Application.Current.Dispatcher.Invoke(ClearSession);
            return null;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void SetSession(UserRecord user, AuthToken token)
    {
        CurrentUser = user;
        CurrentToken = token;
        _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.expiresIn);
        IsAuthenticated = true;
    }

    private void ClearSession()
    {
        CurrentUser = null;
        CurrentToken = null;
        _tokenExpiresAt = DateTimeOffset.MinValue;
        IsAuthenticated = false;
    }

    #region Token Persistence (DPAPI)

    private static void PersistRefreshToken(string refreshToken)
    {
        try
        {
            var data = Encoding.UTF8.GetBytes(refreshToken);
            var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(AppPaths.SessionFilePath, encrypted);
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[Session] Failed to persist token: {ex.Message}");
        }
    }

    private static string? LoadPersistedRefreshToken()
    {
        try
        {
            if (!File.Exists(AppPaths.SessionFilePath))
                return null;

            var encrypted = File.ReadAllBytes(AppPaths.SessionFilePath);
            var data = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[Session] Failed to load persisted token: {ex.Message}");
            return null;
        }
    }

    private static void ClearPersistedData()
    {
        try
        {
            if (File.Exists(AppPaths.SessionFilePath))
            {
                File.Delete(AppPaths.SessionFilePath);
            }
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[Session] Failed to clear persisted token: {ex.Message}");
        }
    }

    #endregion

    public void Dispose()
    {
        _refreshLock.Dispose();
    }
}
