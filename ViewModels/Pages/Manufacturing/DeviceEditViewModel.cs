using System.Text.Json;
using uniffi.stump;
using Poplar.Models;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class DeviceEditViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _deviceType = "scanner"; // scanner, printer, plc

    [ObservableProperty]
    private string _protocol = "serial"; // serial, s7, http

    [ObservableProperty]
    private bool _isPrimary;

    // S7 Specific
    [ObservableProperty] private string _host = "localhost";
    [ObservableProperty] private int _port = 102;
    [ObservableProperty] private int _rack = 0;
    [ObservableProperty] private int _slot = 1;
    [ObservableProperty] private int _dbNumber = 1;
    [ObservableProperty] private int _station = 1;
    [ObservableProperty] private int _hbOffset = 0;
    [ObservableProperty] private int _hbInterval = 5000;

    // Serial Specific
    [ObservableProperty] private string _serialPort = "/dev/ttyUSB0";
    [ObservableProperty] private int _baudRate = 9600;

    public DeviceEditViewModel()
    {
    }

    /// <summary>
    /// Loads an existing device for editing.
    /// </summary>
    public void LoadDevice(DeviceRecord record)
    {
        Name = record.name;
        DeviceType = record.deviceType;
        Protocol = record.protocol;
        IsPrimary = record.isPrimary;

        try
        {
            if (Protocol == "s7")
            {
                var conn = JsonSerializer.Deserialize<S7ConnectConfig>(record.connectConfig);
                if (conn != null)
                {
                    Host = conn.host;
                    Port = conn.port;
                    Rack = conn.rack;
                    Slot = conn.slot;
                    DbNumber = conn.db;
                    Station = conn.station;
                }
                if (!string.IsNullOrEmpty(record.healthConfig))
                {
                    var health = JsonSerializer.Deserialize<S7HealthConfig>(record.healthConfig);
                    if (health != null)
                    {
                        HbOffset = health.offset;
                        HbInterval = health.interval_ms;
                    }
                }
            }
            else if (Protocol == "serial")
            {
                var conn = JsonSerializer.Deserialize<SerialConnectConfig>(record.connectConfig);
                if (conn != null)
                {
                    SerialPort = conn.port;
                    BaudRate = conn.baud;
                }
            }
            else if (Protocol == "http")
            {
                var conn = JsonSerializer.Deserialize<HttpConnectConfig>(record.connectConfig);
                if (conn != null)
                {
                    Host = conn.host;
                    Port = conn.port;
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DeviceEditViewModel] Failed to parse config: {ex.Message}");
        }
    }

    /// <summary>
    /// Constructs a DeviceRecord from the current form state.
    /// </summary>
    public DeviceRecord GetRecord(int id = 0, int workspaceId = 1)
    {
        string connectConfig = "{}";
        string? healthConfig = null;

        if (Protocol == "s7")
        {
            connectConfig = JsonSerializer.Serialize(new S7ConnectConfig { host = Host, port = Port, rack = Rack, slot = Slot, db = DbNumber, station = Station });
            healthConfig = JsonSerializer.Serialize(new S7HealthConfig { offset = HbOffset, interval_ms = HbInterval });
        }
        else if (Protocol == "serial")
        {
            connectConfig = JsonSerializer.Serialize(new SerialConnectConfig { port = SerialPort, baud = BaudRate });
        }
        else if (Protocol == "http")
        {
            connectConfig = JsonSerializer.Serialize(new HttpConnectConfig { host = Host, port = Port });
        }

        return new DeviceRecord(id, Name, DeviceType, Protocol, connectConfig, healthConfig, IsPrimary, workspaceId);
    }

    partial void OnDeviceTypeChanged(string value)
    {
        // Auto-switch protocol based on type
        if (value == "plc") Protocol = "s7";
        else if (value == "scanner") Protocol = "serial";
        else if (value == "printer") Protocol = "http";
    }
}
