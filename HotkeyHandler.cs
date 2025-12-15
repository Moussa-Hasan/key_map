using System.Runtime.InteropServices;

namespace LangFlip
{
    internal class HotkeyHandler : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;

        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;

        private const uint VK_Q = 0x51;

        private IntPtr _windowHandle;
        private bool _disposed = false;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public event EventHandler? HotkeyPressed;

        public HotkeyHandler(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;

            if (!RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_Q))
            {
                throw new InvalidOperationException("Failed to register hotkey. It may already be in use.");
            }
        }

        public void ProcessMessage(Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
                _disposed = true;
            }
        }
    }
}

