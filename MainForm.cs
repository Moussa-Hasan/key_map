using System.Runtime.InteropServices;

namespace LangFlip
{
    internal class MainForm : Form
    {
        private NotifyIcon? _trayIcon;
        private ContextMenuStrip? _trayMenu;
        private ToolStripMenuItem? _startupMenuItem;
        private HotkeyHandler? _hotkeyHandler;
        private HotkeySettings _settings;
        private static readonly Lazy<Icon> _embeddedIcon = new(CreateEmbeddedIcon);

        public MainForm()
        {
            _settings = Settings.Load();
            InitializeComponent();
            InitializeHotkey();
        }

        private void InitializeComponent()
        {
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            Visible = false;
            FormBorderStyle = FormBorderStyle.None;
            Size = new Size(0, 0);

            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("Change Shortcut", null, OnChangeShortcut);
            
            _startupMenuItem = new ToolStripMenuItem("Start with Windows")
            {
                CheckOnClick = true,
                Checked = StartupManager.IsRegistered()
            };
            _startupMenuItem.Click += OnToggleStartup;
            _trayMenu.Items.Add(_startupMenuItem);
            
            _trayMenu.Items.Add("-"); // Separator
            _trayMenu.Items.Add("Exit", null, OnExit);

            _trayIcon = new NotifyIcon
            {
                Icon = _embeddedIcon.Value,
                ContextMenuStrip = _trayMenu,
                Text = "LangFlip active",
                Visible = true
            };

            UpdateTrayIconText();
        }

        private void InitializeHotkey()
        {
            try
            {
                _hotkeyHandler = new HotkeyHandler(Handle, _settings);
                _hotkeyHandler.HotkeyPressed += OnHotkeyPressed;
                UpdateTrayIconText();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"Failed to register hotkey: {ex.Message}\n\nThe application will exit.",
                    "LangFlip Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void UpdateTrayIconText()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Text = $"LangFlip active ({_settings.GetDisplayString()})";
            }
        }

        private void OnChangeShortcut(object? sender, EventArgs e)
        {
            using var form = new HotkeySettingsForm(_settings);
            if (form.ShowDialog() == DialogResult.OK)
            {
                _settings = form.Settings;
                Settings.Save(_settings);

                try
                {
                    _hotkeyHandler?.UpdateHotkey(_settings);
                    UpdateTrayIconText();
                    MessageBox.Show($"Shortcut changed to: {_settings.GetDisplayString()}",
                        "Shortcut Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show($"Failed to register new hotkey: {ex.Message}\n\nPlease try a different combination.",
                        "LangFlip Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // Revert to previous settings
                    _settings = Settings.Load();
                }
            }
        }

        private void OnToggleStartup(object? sender, EventArgs e)
        {
            if (_startupMenuItem == null)
            {
                return;
            }

            bool shouldBeEnabled = _startupMenuItem.Checked;
            bool success = false;

            if (shouldBeEnabled)
            {
                success = StartupManager.Register();
                if (!success)
                {
                    _startupMenuItem.Checked = false;
                    MessageBox.Show("Failed to enable startup with Windows. Please try again or check your permissions.",
                        "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                success = StartupManager.Unregister();
                if (!success)
                {
                    _startupMenuItem.Checked = true;
                    MessageBox.Show("Failed to disable startup with Windows. Please try again or check your permissions.",
                        "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            _hotkeyHandler?.ProcessMessage(m);
        }

        private void OnHotkeyPressed(object? sender, EventArgs e)
        {
            try
            {
                ExecuteCorrectionFlow();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in correction flow: {ex.Message}");
            }
        }

        private void ExecuteCorrectionFlow()
        {
            string? clipboardText = CopySelectionWithRetry();
            if (clipboardText is null)
            {
                return;
            }

            if (string.IsNullOrEmpty(clipboardText))
            {
                return;
            }

            bool containsArabic = TextMapper.ContainsArabic(clipboardText);
            bool convertToArabic = !containsArabic;

            string convertedText = TextMapper.ConvertText(clipboardText, convertToArabic);

            try
            {
                Clipboard.SetText(convertedText);
            }
            catch
            {
                return;
            }

            Thread.Sleep(20);

            // Paste using SendKeys
            try
            {
                SendKeys.SendWait("^v");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Paste failed: {ex.Message}");
            }

            Thread.Sleep(200);

            LanguageSwitch.SwitchLanguage();
        }

        private void OnExit(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hotkeyHandler?.Dispose();
                _trayIcon?.Dispose();
                _trayMenu?.Dispose();
            }
            base.Dispose(disposing);
        }

        private static Icon LoadTrayIcon()
        {
            return _embeddedIcon.Value;
        }

        private static Icon CreateEmbeddedIcon()
        {
            try
            {
                var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon is not null)
                {
                    return new Icon(appIcon, new Size(16, 16));
                }
            }
            catch
            {
            }

            try
            {
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                var iconPath = Path.Combine(exeDir, "LangFlip.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath, new Size(16, 16));
                }
            }
            catch
            {
            }

            try
            {
                using var bmp = new Bitmap(16, 16);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(52, 73, 94));
                    using var font = new Font("Segoe UI", 9, FontStyle.Bold, GraphicsUnit.Pixel);
                    var rect = new RectangleF(0, 0, bmp.Width, bmp.Height);
                    using var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("L", font, Brushes.White, rect, format);
                }

                IntPtr hIcon = bmp.GetHicon();
                var icon = Icon.FromHandle(hIcon);
                var clone = (Icon)icon.Clone();
                DestroyIcon(hIcon);
                return clone;
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private static void SendCtrlCombo(ushort key)
        {
            const uint KEYEVENTF_KEYUP = 0x0002;
            const ushort VK_CONTROL = 0x11;

            INPUT[] inputs = new INPUT[4];

            // Ctrl down
            inputs[0] = new INPUT
            {
                type = 1,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT { wVk = VK_CONTROL, dwFlags = 0 }
                }
            };
            // Key down
            inputs[1] = new INPUT
            {
                type = 1,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT { wVk = key, dwFlags = 0 }
                }
            };
            // Key up
            inputs[2] = new INPUT
            {
                type = 1,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT { wVk = key, dwFlags = KEYEVENTF_KEYUP }
                }
            };
            // Ctrl up
            inputs[3] = new INPUT
            {
                type = 1,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT { wVk = VK_CONTROL, dwFlags = KEYEVENTF_KEYUP }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public INPUTUNION u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private static string? CopySelectionWithRetry()
        {
            string? originalClipboard = null;
            try
            {
                if (Clipboard.ContainsText())
                {
                    originalClipboard = Clipboard.GetText();
                }
            }
            catch
            {
                // If we can't read the clipboard, continue anyway
            }

            string? last = null;

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    SendKeys.SendWait("^a");
                    Thread.Sleep(100);
                    SendKeys.SendWait("^c");
                    Thread.Sleep(150);
                }
                catch
                {
                }

                last = ReadClipboardWithPoll();

                // Only return if something was selected
                if (!string.IsNullOrEmpty(last) && last!.Length > 1)
                {
                    // Check if the clipboard content is different from what we saved
                    if (last != originalClipboard)
                    {
                        return last;
                    }
                }
            }

            // return null if nothing was selected
            return null;
        }

        private static string? ReadClipboardWithPoll()
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        return Clipboard.GetText();
                    }
                }
                catch
                {
                    // ignore and retry
                }
                Thread.Sleep(40);
            }
            return null;
        }
    }
}

