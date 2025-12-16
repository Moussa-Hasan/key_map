using System.Runtime.InteropServices;
using System.Drawing;

namespace LangFlip
{
    internal class HotkeySettingsForm : Form
    {
        // use null-forgiving to satisfy nullable analysis.
        private CheckBox _chkCtrl = null!;
        private CheckBox _chkShift = null!;
        private CheckBox _chkAlt = null!;
        private CheckBox _chkWin = null!;
        private ComboBox _cmbKey = null!;
        private Button _btnOK = null!;
        private Button _btnCancel = null!;
        private Label _lblPreview = null!;

        public HotkeySettings Settings { get; private set; }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public HotkeySettingsForm(HotkeySettings currentSettings)
        {
            Settings = currentSettings;
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            Text = "Change Shortcut";
            Size = new Size(350, 280);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;

            var lblModifiers = new Label
            {
                Text = "Modifiers:",
                Location = new Point(20, 20),
                Size = new Size(100, 20)
            };

            _chkCtrl = new CheckBox
            {
                Text = "Ctrl",
                Location = new Point(20, 45),
                Size = new Size(60, 20)
            };

            _chkShift = new CheckBox
            {
                Text = "Shift",
                Location = new Point(90, 45),
                Size = new Size(60, 20)
            };

            _chkAlt = new CheckBox
            {
                Text = "Alt",
                Location = new Point(160, 45),
                Size = new Size(60, 20)
            };

            _chkWin = new CheckBox
            {
                Text = "Win",
                Location = new Point(230, 45),
                Size = new Size(60, 20)
            };

            var lblKey = new Label
            {
                Text = "Key:",
                Location = new Point(20, 85),
                Size = new Size(100, 20)
            };

            _cmbKey = new ComboBox
            {
                Location = new Point(20, 110),
                Size = new Size(270, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Populate keys
            _cmbKey.Items.AddRange(new object[]
            {
                "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P",
                "A", "S", "D", "F", "G", "H", "J", "K", "L",
                "Z", "X", "C", "V", "B", "N", "M",
                "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
                "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12"
            });

            _lblPreview = new Label
            {
                Text = "Preview: ",
                Location = new Point(20, 150),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            _btnOK = new Button
            {
                Text = "OK",
                Location = new Point(130, 200),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };

            _btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(215, 200),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };

            Controls.AddRange(new Control[]
            {
                lblModifiers, _chkCtrl, _chkShift, _chkAlt, _chkWin,
                lblKey, _cmbKey, _lblPreview, _btnOK, _btnCancel
            });

            _chkCtrl.CheckedChanged += UpdatePreview;
            _chkShift.CheckedChanged += UpdatePreview;
            _chkAlt.CheckedChanged += UpdatePreview;
            _chkWin.CheckedChanged += UpdatePreview;
            _cmbKey.SelectedIndexChanged += UpdatePreview;

            AcceptButton = _btnOK;
            CancelButton = _btnCancel;
        }

        private void LoadSettings()
        {
            _chkCtrl.Checked = Settings.UseCtrl;
            _chkShift.Checked = Settings.UseShift;
            _chkAlt.Checked = Settings.UseAlt;
            _chkWin.Checked = Settings.UseWin;

            var keyName = GetKeyName(Settings.VirtualKey);
            var index = _cmbKey.Items.IndexOf(keyName);
            if (index >= 0)
            {
                _cmbKey.SelectedIndex = index;
            }
            else
            {
                _cmbKey.SelectedIndex = 0;
            }

            UpdatePreview();
        }

        private void UpdatePreview(object? sender, EventArgs? e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var parts = new List<string>();
            if (_chkCtrl.Checked) parts.Add("Ctrl");
            if (_chkShift.Checked) parts.Add("Shift");
            if (_chkAlt.Checked) parts.Add("Alt");
            if (_chkWin.Checked) parts.Add("Win");

            if (_cmbKey.SelectedItem != null)
            {
                parts.Add(_cmbKey.SelectedItem.ToString() ?? "");
            }

            _lblPreview.Text = "Preview: " + string.Join(" + ", parts);
        }

        private static string GetKeyName(uint vk)
        {
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
                _ => "Q"
            };
        }

        private static uint GetVirtualKey(string keyName)
        {
            return keyName switch
            {
                "A" => 0x41, "B" => 0x42, "C" => 0x43, "D" => 0x44, "E" => 0x45,
                "F" => 0x46, "G" => 0x47, "H" => 0x48, "I" => 0x49, "J" => 0x4A,
                "K" => 0x4B, "L" => 0x4C, "M" => 0x4D, "N" => 0x4E, "O" => 0x4F,
                "P" => 0x50, "Q" => 0x51, "R" => 0x52, "S" => 0x53, "T" => 0x54,
                "U" => 0x55, "V" => 0x56, "W" => 0x57, "X" => 0x58, "Y" => 0x59,
                "Z" => 0x5A,
                "0" => 0x30, "1" => 0x31, "2" => 0x32, "3" => 0x33, "4" => 0x34,
                "5" => 0x35, "6" => 0x36, "7" => 0x37, "8" => 0x38, "9" => 0x39,
                "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73,
                "F5" => 0x74, "F6" => 0x75, "F7" => 0x76, "F8" => 0x77,
                "F9" => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
                _ => 0x51 // Default to Q
            };
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                if (!_chkCtrl.Checked && !_chkShift.Checked && !_chkAlt.Checked && !_chkWin.Checked)
                {
                    MessageBox.Show("Please select at least one modifier key.", "Invalid Shortcut",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }

                if (_cmbKey.SelectedItem == null)
                {
                    MessageBox.Show("Please select a key.", "Invalid Shortcut",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }

                Settings.UseCtrl = _chkCtrl.Checked;
                Settings.UseShift = _chkShift.Checked;
                Settings.UseAlt = _chkAlt.Checked;
                Settings.UseWin = _chkWin.Checked;
                Settings.VirtualKey = GetVirtualKey(_cmbKey.SelectedItem.ToString() ?? "Q");
            }
            base.OnFormClosing(e);
        }
    }
}

