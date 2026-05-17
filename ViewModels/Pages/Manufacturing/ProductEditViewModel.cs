using Poplar.Services;
using uniffi.stump;
using System.Collections.ObjectModel;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class ProductEditViewModel : ObservableObject
{
    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DeviceSelectionItem> _availableDevices = new();

    public async Task InitializeAsync(DeviceService deviceService)
    {
        var devices = await deviceService.GetDevicesAsync();
        foreach (var d in devices)
        {
            AvailableDevices.Add(new DeviceSelectionItem(d));
        }
    }

    public string[] GetSelectedDeviceNames()
    {
        return AvailableDevices
            .Where(x => x.IsSelected)
            .Select(x => x.Device.name)
            .ToArray();
    }
}

public partial class DeviceSelectionItem : ObservableObject
{
    public DeviceRecord Device { get; }
    
    [ObservableProperty]
    private bool _isSelected;

    public int id => Device.id;
    public string name => Device.name;
    public string protocol => Device.protocol?.ToUpper() ?? string.Empty;
    public string deviceType => Device.deviceType;

    public DeviceSelectionItem(DeviceRecord device)
    {
        Device = device;
    }
}
