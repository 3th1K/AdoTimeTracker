using Microsoft.Win32;

namespace AdoTimeTracker.Core.Services;

public class StartupService
{
    private const string AppName = "AdoTimeTracker";
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly LogService _logService;

    public StartupService(LogService logService)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _logService.Info("StartupService initialized");
    }

    public bool IsEnabled()
    {
        _logService.Info("Checking if startup is enabled");

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);

            if (key == null)
            {
                _logService.Error($"Failed to open registry key: {RegistryPath}");
                throw new InvalidOperationException($"Unable to access registry key: {RegistryPath}");
            }

            var value = key.GetValue(AppName);
            var isEnabled = value != null;

            _logService.Info($"Startup is {(isEnabled ? "enabled" : "disabled")}");

            return isEnabled;
        }
        catch (System.Security.SecurityException ex)
        {
            _logService.Error($"Security error while checking startup status: {ex.Message}");
            throw new InvalidOperationException($"Access denied to registry: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logService.Error($"Unauthorized access while checking startup status: {ex.Message}");
            throw new InvalidOperationException($"Access denied to registry: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            _logService.Error($"I/O error while checking startup status: {ex.Message}");
            throw new InvalidOperationException($"Failed to read registry: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logService.Error($"Unexpected error while checking startup status: {ex.Message}");
            throw new InvalidOperationException($"Failed to check startup status: {ex.Message}", ex);
        }
    }

    public void Enable()
    {
        _logService.Info("Enabling startup");

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                RegistryPath,
                true);

            if (key == null)
            {
                _logService.Error($"Failed to open registry key for writing: {RegistryPath}");
                throw new InvalidOperationException($"Unable to access registry key: {RegistryPath}");
            }

            var executablePath = Application.ExecutablePath;

            if (string.IsNullOrWhiteSpace(executablePath))
            {
                _logService.Error("Application executable path is null or empty");
                throw new InvalidOperationException("Unable to determine application executable path");
            }

            _logService.Info($"Setting startup registry value to: {executablePath}");

            key.SetValue(
                AppName,
                executablePath);

            _logService.Info("Startup enabled successfully");
        }
        catch (System.Security.SecurityException ex)
        {
            _logService.Error($"Security error while enabling startup: {ex.Message}");
            throw new InvalidOperationException($"Access denied to registry: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logService.Error($"Unauthorized access while enabling startup: {ex.Message}");
            throw new InvalidOperationException($"Write access denied to registry: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            _logService.Error($"I/O error while enabling startup: {ex.Message}");
            throw new InvalidOperationException($"Failed to write to registry: {ex.Message}", ex);
        }
        catch (ArgumentException ex)
        {
            _logService.Error($"Invalid argument while enabling startup: {ex.Message}");
            throw new InvalidOperationException($"Invalid registry value: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logService.Error($"Unexpected error while enabling startup: {ex.Message}");
            throw new InvalidOperationException($"Failed to enable startup: {ex.Message}", ex);
        }
    }

    public void Disable()
    {
        _logService.Info("Disabling startup");

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                RegistryPath,
                true);

            if (key == null)
            {
                _logService.Error($"Failed to open registry key for writing: {RegistryPath}");
                throw new InvalidOperationException($"Unable to access registry key: {RegistryPath}");
            }

            _logService.Info($"Deleting startup registry value: {AppName}");

            key.DeleteValue(
                AppName,
                false);

            _logService.Info("Startup disabled successfully");
        }
        catch (System.Security.SecurityException ex)
        {
            _logService.Error($"Security error while disabling startup: {ex.Message}");
            throw new InvalidOperationException($"Access denied to registry: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logService.Error($"Unauthorized access while disabling startup: {ex.Message}");
            throw new InvalidOperationException($"Write access denied to registry: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            _logService.Error($"I/O error while disabling startup: {ex.Message}");
            throw new InvalidOperationException($"Failed to write to registry: {ex.Message}", ex);
        }
        catch (ArgumentException ex)
        {
            _logService.Error($"Invalid argument while disabling startup: {ex.Message}");
            throw new InvalidOperationException($"Invalid registry value: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logService.Error($"Unexpected error while disabling startup: {ex.Message}");
            throw new InvalidOperationException($"Failed to disable startup: {ex.Message}", ex);
        }
    }
}