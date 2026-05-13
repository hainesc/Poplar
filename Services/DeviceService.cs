using CommunityToolkit.Mvvm.Messaging;
using uniffi.stump;
using Poplar.Models;
using System.Diagnostics;

namespace Poplar.Services;

/// <summary>
/// Service for managing discrete manufacturing devices.
/// Wraps FFI calls and handles real-time status updates via DeviceObserver.
/// </summary>
public sealed class DeviceService : DeviceObserver, IDisposable
{
    private readonly BackendService _backend;

    public DeviceService(BackendService backend)
    {
        _backend = backend;
    }

    /// <summary>
    /// Initializes the backend and starts observing device health.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (!_backend.IsInitialized)
        {
            await _backend.InitializeAsync();
        }

        // Subscribe to real-time updates from Rust
        await _backend.DeviceVm.SubscribeHealth(this);
        Debug.WriteLine("[DeviceService] Subscribed to device health updates.");
    }

    public async Task<DeviceRecord[]> GetDevicesAsync()
    {
        return await _backend.DeviceVm.GetAllDevices();
    }

    public async Task AddDeviceAsync(DeviceRecord record)
    {
        await _backend.DeviceVm.AddDevice(record);
        Debug.WriteLine($"[DeviceService] Added device: {record.name}");
    }

    public async Task UpdateDeviceAsync(DeviceRecord record)
    {
        await _backend.DeviceVm.UpdateDevice(record);
        Debug.WriteLine($"[DeviceService] Updated device: {record.name}");
    }

    public async Task DeleteDeviceAsync(int id)
    {
        await _backend.DeviceVm.DeleteDevice(id);
        Debug.WriteLine($"[DeviceService] Deleted device ID: {id}");
    }

    /// <summary>
    /// Callback from Rust when device statuses change.
    /// </summary>
    public void OnDevicesHealthChanged(DeviceStatus[] report)
    {
        Debug.WriteLine($"[DeviceService] Received health update for {report.Length} devices.");
        foreach (var status in report)
        {
            Debug.WriteLine($"  -> Device ID: {status.id}, Name: {status.name}, Online: {status.isOnline}, Msg: {status.message}");
        }

        // Broadcast the update to any interested ViewModels
        WeakReferenceMessenger.Default.Send(new DeviceStatusChangedMessage(report));
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
