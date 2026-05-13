using uniffi.stump;

namespace Poplar.Services;

/// <summary>
/// Manages the lifecycle of the stump FFI backend.
/// Initializes the Launcher and exposes ViewModel instances for DI consumption.
/// </summary>
public sealed class BackendService : IDisposable
{
    private Launcher? _launcher;
    private RbacViewModel? _rbacVm;
    private DeviceViewModel? _deviceVm;
    private ProductViewModel? _productVm;
    private bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Whether the backend has been successfully initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// The RBAC ViewModel for authentication and authorization operations.
    /// </summary>
    public RbacViewModel RbacVm => _rbacVm
        ?? throw new InvalidOperationException("Backend not initialized. Call InitializeAsync first.");

    /// <summary>
    /// The Device ViewModel for device management operations.
    /// </summary>
    public DeviceViewModel DeviceVm => _deviceVm
        ?? throw new InvalidOperationException("Backend not initialized. Call InitializeAsync first.");

    /// <summary>
    /// The Product ViewModel for product and process configuration.
    /// </summary>
    public ProductViewModel ProductVm => _productVm
        ?? throw new InvalidOperationException("Backend not initialized. Call InitializeAsync first.");

    /// <summary>
    /// The raw Launcher instance for accessing other ViewModels if needed.
    /// </summary>
    public Launcher Launcher => _launcher
        ?? throw new InvalidOperationException("Backend not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Initializes the stump backend runtime. Safe to call multiple times (idempotent).
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            Debug.WriteLine($"[Backend] Initializing: dbUrl={AppPaths.DatabaseUrl}, config={AppPaths.ConfigFilePath}");

            _launcher = await Launcher.LauncherAsync(AppPaths.DatabaseUrl, AppPaths.ConfigFilePath);
            _rbacVm = _launcher.RbacVm();
            _deviceVm = _launcher.DeviceManager();
            _productVm = _launcher.ProductVm();

            _isInitialized = true;
            Debug.WriteLine("[Backend] Initialized successfully.");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public void Dispose()
    {
        _rbacVm?.Dispose();
        _deviceVm?.Dispose();
        _productVm?.Dispose();
        _launcher?.Dispose();
        _initLock.Dispose();
    }
}
