using System;
using System.IO;

namespace Poplar;

/// <summary>
/// Centralized path management for application data, database, and configuration files.
/// Uses %LocalAppData%/Poplar on Windows for user-writable storage.
/// </summary>
internal static class AppPaths
{
    /// <summary>
    /// Root data directory: %LocalAppData%/Poplar
    /// </summary>
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) ?? AppContext.BaseDirectory,
        "Poplar"
    );

    /// <summary>
    /// SQLite database URL for stump runtime.
    /// </summary>
    public static string DatabaseUrl
    {
        get
        {
            var dbPath = Path.Combine(DataDirectory, "stump.db").Replace("\\", "/");
            return $"sqlite://{dbPath}?mode=rwc";
        }
    }

    /// <summary>
    /// Path to the stump configuration file (shipped alongside the executable).
    /// </summary>
    public static string ConfigFilePath => Path.Combine(AppContext.BaseDirectory, "config.toml");

    /// <summary>
    /// Path for encrypted session token persistence.
    /// </summary>
    public static string SessionFilePath => Path.Combine(DataDirectory, "session.dat");

    /// <summary>
    /// Application log directory.
    /// </summary>
    public static string LogPath => Path.Combine(DataDirectory, "logs");

    static AppPaths()
    {
        // Ensure data directory exists
        if (!Directory.Exists(DataDirectory))
        {
            Directory.CreateDirectory(DataDirectory);
        }

        // Ensure log directory exists
        if (!Directory.Exists(LogPath))
        {
            Directory.CreateDirectory(LogPath);
        }
    }
}
