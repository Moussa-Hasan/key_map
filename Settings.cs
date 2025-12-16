using System.Text.Json;

namespace LangFlip
{
    internal class HotkeySettings
    {
        // Default shortcut: Shift + Win + E
        public bool UseCtrl { get; set; } = false;
        public bool UseShift { get; set; } = true;
        public bool UseAlt { get; set; } = false;
        public bool UseWin { get; set; } = true;
        public uint VirtualKey { get; set; } = 0x45; // VK_E

        public static HotkeySettings Default => new();

        public string GetDisplayString()
        {
            var parts = new List<string>();
            if (UseCtrl) parts.Add("Ctrl");
            if (UseShift) parts.Add("Shift");
            if (UseAlt) parts.Add("Alt");
            if (UseWin) parts.Add("Win");
            
            var keyName = GetKeyName(VirtualKey);
            parts.Add(keyName);
            
            return string.Join(" + ", parts);
        }

        private static string GetKeyName(uint vk)
        {
            // Common virtual key codes
            return vk switch
            {
                0x41 => "A", 0x42 => "B", 0x43 => "C", 0x44 => "D", 0x45 => "E",
                0x46 => "F", 0x47 => "G", 0x48 => "H", 0x49 => "I", 0x4A => "J",
                0x4B => "K", 0x4C => "L", 0x4D => "M", 0x4E => "N", 0x4F => "O",
                0x50 => "P", 0x51 => "Q", 0x52 => "R", 0x53 => "S", 0x54 => "T",
                0x55 => "U", 0x56 => "V", 0x57 => "W", 0x58 => "X", 0x59 => "Y",
                0x5A => "Z",
                0x30 => "0", 0x31 => "1", 0x32 => "2", 0x33 => "3", 0x34 => "4",
                0x35 => "5", 0x36 => "6", 0x37 => "7", 0x38 => "8", 0x39 => "9",
                0x70 => "F1", 0x71 => "F2", 0x72 => "F3", 0x73 => "F4",
                0x74 => "F5", 0x75 => "F6", 0x76 => "F7", 0x77 => "F8",
                0x78 => "F9", 0x79 => "F10", 0x7A => "F11", 0x7B => "F12",
                _ => $"VK_{vk:X2}"
            };
        }

        public uint GetModifiers()
        {
            uint mod = 0;
            if (UseCtrl) mod |= 0x0002; // MOD_CONTROL
            if (UseShift) mod |= 0x0004; // MOD_SHIFT
            if (UseAlt) mod |= 0x0001; // MOD_ALT
            if (UseWin) mod |= 0x0008; // MOD_WIN
            return mod;
        }
    }

    internal static class Settings
    {
        private static string GetSettingsPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var settingsDir = Path.Combine(appData, "LangFlip");
            Directory.CreateDirectory(settingsDir);
            return Path.Combine(settingsDir, "settings.json");
        }

        public static HotkeySettings Load()
        {
            try
            {
                var path = GetSettingsPath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<HotkeySettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch
            {
                // If loading fails, return default settings
            }
            return HotkeySettings.Default;
        }

        public static void Save(HotkeySettings settings)
        {
            try
            {
                var path = GetSettingsPath();
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch
            {
                // If saving fails, silently ignore
            }
        }
    }
}

