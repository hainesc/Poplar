using CommunityToolkit.Mvvm.Messaging;
using uniffi.stump;
using Poplar.Models;
using Poplar.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class DevicesViewModel : ObservableObject, IRecipient<DeviceStatusChangedMessage>
{
    private readonly DeviceService _deviceService;
    private bool _isInitialized;

    [ObservableProperty]
    private ObservableCollection<DeviceItemViewModel> _devices = new();

    public DevicesViewModel(DeviceService deviceService)
    {
        _deviceService = deviceService;
        
        // Register to receive real-time status updates
        WeakReferenceMessenger.Default.Register(this);
    }

    /// <summary>
    /// Called when the page is navigated to.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            await _deviceService.InitializeAsync();
            var devices = await _deviceService.GetDevicesAsync();
            
            Devices.Clear();
            foreach (var device in devices)
            {
                Devices.Add(new DeviceItemViewModel(device));
            }

            _isInitialized = true;
            Debug.WriteLine($"[DevicesViewModel] Loaded {Devices.Count} devices.");
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[DevicesViewModel] Failed to initialize: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles status updates from the DeviceService.
    /// </summary>
    public void Receive(DeviceStatusChangedMessage message)
    {
        // Update the statuses on the UI thread
        App.Current.Dispatcher.Invoke(() =>
        {
            foreach (var status in message.Statuses)
            {
                var device = Devices.FirstOrDefault(d => d.Record.id == status.id);
                if (device != null)
                {
                    device.Status = status;
                }
            }
        });
    }

    [RelayCommand]
    private async Task OnDeleteDevice(DeviceItemViewModel deviceItem)
    {
        if (deviceItem == null) return;

        try
        {
            await _deviceService.DeleteDeviceAsync(deviceItem.Record.id);
            Devices.Remove(deviceItem);
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[DevicesViewModel] Delete failed: {ex.Message}");
        }
    }
}
