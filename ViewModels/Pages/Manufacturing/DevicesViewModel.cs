using CommunityToolkit.Mvvm.Messaging;
using uniffi.stump;
using Poplar.Models;
using Poplar.Services;
using Poplar.Views.Pages.Manufacturing;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class DevicesViewModel : ObservableObject, IRecipient<DeviceStatusChangedMessage>
{
    private readonly DeviceService _deviceService;
    private readonly IContentDialogService _dialogService;
    private readonly IServiceProvider _serviceProvider;
    private bool _isInitialized;

    [ObservableProperty]
    private ObservableCollection<DeviceItemViewModel> _devices = new();

    public DevicesViewModel(
        DeviceService deviceService, 
        IContentDialogService dialogService,
        IServiceProvider serviceProvider)
    {
        _deviceService = deviceService;
        _dialogService = dialogService;
        _serviceProvider = serviceProvider;
        
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
    private async Task OnAddDevice()
    {
        var editVm = new DeviceEditViewModel();
        var content = new DeviceEditControl(editVm);

        var result = await _dialogService.ShowSimpleDialogAsync(new SimpleContentDialogCreateOptions
        {
            Title = "Register New Device Node",
            Content = content,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel"
        });

        if (result == ContentDialogResult.Primary)
        {
            try
            {
                // For now use default workspaceId = 1
                var record = editVm.GetRecord(workspaceId: 1);
                await _deviceService.AddDeviceAsync(record);
                
                // Refresh list
                var allDevices = await _deviceService.GetDevicesAsync();
                Devices.Clear();
                foreach (var d in allDevices) Devices.Add(new DeviceItemViewModel(d));
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"[DevicesViewModel] Add failed: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task OnEditDevice(DeviceItemViewModel deviceItem)
    {
        if (deviceItem == null) return;

        var editVm = new DeviceEditViewModel();
        editVm.LoadDevice(deviceItem.Record);
        var content = new DeviceEditControl(editVm);

        var result = await _dialogService.ShowSimpleDialogAsync(new SimpleContentDialogCreateOptions
        {
            Title = "Edit Device Node",
            Content = content,
            PrimaryButtonText = "Update",
            CloseButtonText = "Cancel"
        });

        if (result == ContentDialogResult.Primary)
        {
            try
            {
                var record = editVm.GetRecord(deviceItem.Record.id, deviceItem.Record.workspaceId);
                await _deviceService.UpdateDeviceAsync(record);
                
                // Update local item
                deviceItem.Record = record;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"[DevicesViewModel] Update failed: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task OnDeleteDevice(DeviceItemViewModel deviceItem)
    {
        if (deviceItem == null) return;

        var result = await _dialogService.ShowSimpleDialogAsync(new SimpleContentDialogCreateOptions
        {
            Title = "Delete Device",
            Content = $"Are you sure you want to delete '{deviceItem.Record.name}'? This action cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel"
        });

        if (result == ContentDialogResult.Primary)
        {
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
}
