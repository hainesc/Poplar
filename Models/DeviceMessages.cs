using uniffi.stump;

namespace Poplar.Models;

/// <summary>
/// Message broadcast when device health statuses are updated from the native backend.
/// </summary>
public record DeviceStatusChangedMessage(DeviceStatus[] Statuses);
