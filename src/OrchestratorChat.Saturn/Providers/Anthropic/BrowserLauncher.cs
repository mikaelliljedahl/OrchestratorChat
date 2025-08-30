using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OrchestratorChat.Saturn.Providers.Anthropic;

/// <summary>
/// Cross-platform browser launcher utility
/// </summary>
public static class BrowserLauncher
{
    /// <summary>
    /// Opens a URL in the default browser
    /// </summary>
    /// <param name="url">URL to open</param>
    /// <returns>True if successful, false otherwise</returns>
    public static bool OpenUrl(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: Use shell execute
                var processInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = url
                };
                Process.Start(processInfo);
                return true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: Use xdg-open
                var processInfo = new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = url,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                Process.Start(processInfo);
                return true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS: Use open
                var processInfo = new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = url,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                Process.Start(processInfo);
                return true;
            }
            else
            {
                // Unsupported platform
                return false;
            }
        }
        catch
        {
            // Failed to launch browser
            return false;
        }
    }
}