using Microsoft.Win32;
using System.Windows.Forms;

namespace LangFlip
{
    internal static class StartupManager
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "LangFlip";

        public static bool IsRegistered()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
                if (key == null)
                {
                    return false;
                }

                var value = key.GetValue(AppName) as string;
                if (string.IsNullOrEmpty(value))
                {
                    return false;
                }

                // Compare paths to detect if executable was moved
                var currentPath = GetExecutablePath();
                var registryPath = value.Trim('"');
                
                return string.Equals(currentPath, registryPath, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking startup registration: {ex.Message}");
                return false;
            }
        }

        public static bool Register()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
                if (key == null)
                {
                    return false;
                }

                var executablePath = GetExecutablePath();
                // Quote path to handle spaces in directory names
                var quotedPath = $"\"{executablePath}\"";
                
                key.SetValue(AppName, quotedPath, RegistryValueKind.String);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering startup: {ex.Message}");
                return false;
            }
        }

        public static bool Unregister()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
                if (key == null)
                {
                    return false;
                }

                key.DeleteValue(AppName, false);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error unregistering startup: {ex.Message}");
                return false;
            }
        }

        private static string GetExecutablePath()
        {
            // Try Application.ExecutablePath first (most reliable for WinForms)
            try
            {
                var path = Application.ExecutablePath;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }
            catch
            {
            }

            // Fallback to Process.MainModule
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var module = process.MainModule;
                if (module != null && !string.IsNullOrEmpty(module.FileName))
                {
                    return Path.GetFullPath(module.FileName);
                }
            }
            catch
            {
            }

            // Last resort: use entry assembly location
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            if (assembly != null && !string.IsNullOrEmpty(assembly.Location))
            {
                return Path.GetFullPath(assembly.Location);
            }

            throw new InvalidOperationException("Unable to determine executable path");
        }
    }
}
