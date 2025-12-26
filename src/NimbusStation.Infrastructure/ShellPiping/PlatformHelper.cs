using System.Runtime.InteropServices;

namespace NimbusStation.Infrastructure.ShellPiping;

/// <summary>
/// Provides platform detection and shell configuration for cross-platform shell delegation.
/// </summary>
public static class PlatformHelper
{
    /// <summary>
    /// Gets whether the current platform is Windows.
    /// </summary>
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Gets whether the current platform is Linux.
    /// </summary>
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Gets whether the current platform is macOS.
    /// </summary>
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// Gets whether the current platform is Unix-like (Linux or macOS).
    /// </summary>
    public static bool IsUnix => IsLinux || IsMacOS;

    /// <summary>
    /// Gets the default shell executable and argument for the current platform.
    /// </summary>
    /// <returns>
    /// A tuple containing the shell executable path and the argument to pass a command string.
    /// On Unix: ("/bin/sh", "-c"). On Windows: ("pwsh", "-Command") or ("powershell", "-Command").
    /// </returns>
    public static (string Shell, string Argument) GetDefaultShell()
    {
        if (IsWindows)
            return GetWindowsShell();

        return ("/bin/sh", "-c");
    }

    private static (string Shell, string Argument) GetWindowsShell()
    {
        // Prefer PowerShell Core (pwsh) if available, fall back to Windows PowerShell
        var pwshPath = FindExecutableInPath("pwsh.exe") ?? FindExecutableInPath("pwsh");
        if (pwshPath is not null)
            return (pwshPath, "-Command");

        // Fall back to Windows PowerShell (always available on modern Windows)
        return ("powershell", "-Command");
    }

    private static string? FindExecutableInPath(string executableName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;

        var separator = IsWindows ? ';' : ':';
        var paths = pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path, executableName);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }
}
