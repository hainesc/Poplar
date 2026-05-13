namespace Poplar.Models;

public class S7ConnectConfig
{
    public string host { get; set; } = "localhost";
    public int port { get; set; } = 102;
    public int rack { get; set; } = 0;
    public int slot { get; set; } = 1;
    public int station { get; set; } = 1;
    public int db { get; set; } = 1;
}

public class S7HealthConfig
{
    public int offset { get; set; } = 0;
    public int interval_ms { get; set; } = 5000;
}

public class SerialConnectConfig
{
    public string port { get; set; } = "/dev/ttyUSB0";
    public int baud { get; set; } = 9600;
}

public class HttpConnectConfig
{
    public string host { get; set; } = "localhost";
    public int port { get; set; } = 8080;
}
