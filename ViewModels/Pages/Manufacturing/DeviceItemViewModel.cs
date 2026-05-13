using uniffi.stump;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class DeviceItemViewModel : ObservableObject
{
    [ObservableProperty]
    private DeviceRecord _record;

    [ObservableProperty]
    private DeviceStatus? _status;

    public DeviceItemViewModel(DeviceRecord record)
    {
        _record = record;
    }
}
