## LangFlip – Developer Documentation & Contributor Guide

### 1. Project Overview

LangFlip is a small Windows tray application that lets you **quickly fix text typed in the wrong keyboard layout** (Arabic vs English) using a **global shortcut**.

When the hotkey is pressed:

1. The app selects and copies the current text.
2. It detects whether the text is Arabic or English.
3. It converts the text to the **other** layout (English ↔ Arabic) using a key‑by‑key mapping.
4. It pastes the converted text back.
5. It asks the active window to switch to the **next input language** (so your OS keyboard layout follows).

The app runs **in the background only**, with:
- A tray icon.
- A tray menu to change the hotkey or exit.
- A dialog to configure which key combination triggers the correction.

---

### 2. High-Level Architecture

#### 2.1 Main Components

- **`Program`**: Application entry point, starts `MainForm`.
- **`MainForm`**: Hidden main window.
  - Owns the tray icon and context menu.
  - Loads/saves hotkey settings.
  - Owns `HotkeyHandler` and reacts when the hotkey is pressed.
- **`HotkeyHandler`**:
  - Wraps `RegisterHotKey` / `UnregisterHotKey` (Win32 API).
  - Listens for `WM_HOTKEY` and raises a `.HotkeyPressed` event.
- **`HotkeySettings` & `Settings`**:
  - `HotkeySettings`: in‑memory hotkey model (Ctrl/Shift/Alt/Win + key).
  - `Settings`: loads/saves `HotkeySettings` as JSON in `%APPDATA%/LangFlip/settings.json`.
- **`HotkeySettingsForm`**:
  - A WinForms dialog to let the user choose modifiers and key.
  - Updates `HotkeySettings` if the user clicks OK.
- **`TextMapper`**:
  - Performs **character-by-character conversion** between English and Arabic layouts.
  - Knows whether text currently contains Arabic characters.
- **`LanguageSwitch`**:
  - Uses Win32 messages to ask the foreground window to switch to the next input language.

---

### 3. How the System Works (Step-by-Step Flow)

#### 3.1 Application Startup

1. **`Program.Main`** (in `Program.cs`):
   - Sets visual styles (`Application.EnableVisualStyles`, etc.).
   - Starts the message loop with `Application.Run(new MainForm())`.

2. **`MainForm` constructor** (in `MainForm.cs`):
   - Calls `Settings.Load()` to load the last used hotkey (or default).
   - Calls `InitializeComponent()` to:
     - Hide the form (no visible main window).
     - Set up the tray icon and context menu.
   - Calls `InitializeHotkey()` to register the global hotkey.

#### 3.2 Tray Icon and Menu

Inside `MainForm.InitializeComponent`:

- The form is effectively invisible:
  - `WindowState = Minimized`
  - `ShowInTaskbar = false`
  - `Visible = false`
  - `FormBorderStyle = None`
  - Size set to `0x0`.

- Tray menu items:
  - **"Change Shortcut"** → opens `HotkeySettingsForm`.
  - **Separator**.
  - **"Exit"** → exits the application.

- Tray icon:
  - Icon loaded from:
    - Application icon (`LangFlip.exe`) if possible, or
    - `LangFlip.ico` in the executable directory, or
    - A fallback 16x16 bitmap with an “L”, or
    - `SystemIcons.Application` as a last resort.
  - Tooltip text shows current hotkey, e.g. `"LangFlip active (Shift + Win + E)"`.

#### 3.3 Hotkey Registration and Handling

- `MainForm.InitializeHotkey`:
  - Creates a `HotkeyHandler` with:
    - The form’s window handle (`Handle`), and
    - The current `HotkeySettings`.
  - Subscribes to `HotkeyHandler.HotkeyPressed`.

- `HotkeyHandler`:
  - Calls `RegisterHotKey` from `user32.dll` with:
    - A fixed id (`HOTKEY_ID`).
    - Modifier flags from `HotkeySettings.GetModifiers()`.
    - The virtual key code (`VirtualKey`).
  - If registration fails, it throws an `InvalidOperationException` (e.g., hotkey already in use).

- `MainForm.WndProc` override:
  - For every Windows message:
    - Calls `base.WndProc`.
    - Then passes the message to `_hotkeyHandler?.ProcessMessage(m)`.

- `HotkeyHandler.ProcessMessage`:
  - Checks for `WM_HOTKEY` with the matching `HOTKEY_ID`.
  - If matched, raises the `.HotkeyPressed` event.

- `MainForm.OnHotkeyPressed`:
  - Runs `ExecuteCorrectionFlow()` inside a try/catch (errors are logged to debug output only).

#### 3.4 Correction Flow (Copy → Convert → Paste → Switch Layout)

`MainForm.ExecuteCorrectionFlow`:

1. **Copy selection**:
   - Calls `CopySelectionWithRetry()`:
     - Optionally reads the current clipboard text (so it can detect if selection actually changed).
     - Simulates `Ctrl+A` then `Ctrl+C` using `SendKeys`.
     - Repeats up to 3 times with small delays.
     - Uses `ReadClipboardWithPoll()` to:
       - Retry reading clipboard content up to a few times.
       - Returns the new clipboard content if it’s non-empty, different from the original clipboard, and has length > 1.
     - If no valid text is obtained, returns `null`.

2. **Abort if no text**:
   - If clipboardText is `null` or empty, the flow ends.

3. **Decide direction** (Arabic ↔ English):
   - `TextMapper.ContainsArabic(clipboardText)`:
     - Scans for Unicode characters in the Arabic range (`0x0600`–`0x06FF`).
   - If **contains Arabic**, convert **to English**.
   - If **does not contain Arabic**, convert **to Arabic**.

4. **Convert text**:
   - `TextMapper.ConvertText(clipboardText, convertToArabic)`:
     - If `toArabic = true`:
       - For each character:
         - Special case: `'b'` or `'B'` → outputs **two chars**: `'ل'` then `'ا'` (lam-alif).
         - Else if key exists in `EnglishToArabic`, map to Arabic char.
         - Else keep original char.
     - If `toArabic = false`:
       - Iterates over chars with index:
         - If sees `'ل'` followed by `'ا'`, outputs `'b'` and skips the next char.
         - Else if char exists in `ArabicToEnglish`, output mapped char.
         - Else keep char.

5. **Update clipboard and paste**:
   - Tries to set `Clipboard.SetText(convertedText)`.
     - On failure, aborts.
   - Sleeps briefly (20 ms) to let clipboard update.
   - Uses `SendKeys.SendWait("^v")` to paste.
   - Sleeps 200 ms to give the target app time to apply the paste.

6. **Switch input language**:
   - Calls `LanguageSwitch.SwitchLanguage()`:
     - Gets foreground window (`GetForegroundWindow`).
     - Posts `WM_INPUTLANGCHANGEREQUEST` with `INPUTLANGCHANGE_FORWARD` to request switching to the next input language.

---

### 4. Data & Settings

#### 4.1 HotkeySettings

`HotkeySettings` represents a single global shortcut:

- **Properties**:
  - `UseCtrl`, `UseShift`, `UseAlt`, `UseWin` (bool flags).
  - `VirtualKey` (uint virtual key code, e.g. `0x45` for `E`).
- **Defaults**:
  - Shift + Win + E (no Ctrl, no Alt).
- **Methods**:
  - `GetDisplayString()` → human-readable string (e.g. `"Shift + Win + E"`).
  - `GetModifiers()` → bitmask for `RegisterHotKey`:
    - `MOD_CONTROL` = `0x0002`
    - `MOD_SHIFT`   = `0x0004`
    - `MOD_ALT`     = `0x0001`
    - `MOD_WIN`     = `0x0008`

#### 4.2 Settings Persistence

`Settings` is a static helper:

- **Storage location**:
  - `%APPDATA%/LangFlip/settings.json`
- **Load** (`Settings.Load()`):
  - If file exists, read JSON and deserialize `HotkeySettings`.
  - On any error or null result, returns `HotkeySettings.Default`.
- **Save** (`Settings.Save(HotkeySettings)`):
  - Serializes with indentation.
  - Writes to the settings path.
  - Swallows exceptions (no user-facing error on failure).

---

### 5. UI: Hotkey Settings Form

`HotkeySettingsForm` is a small WinForms dialog:

- **Controls**:
  - Checkboxes: `_chkCtrl`, `_chkShift`, `_chkAlt`, `_chkWin`.
  - Combo box `_cmbKey` with:
    - Letters `A–Z`.
    - Digits `0–9`.
    - `F1–F12`.
  - Label `_lblPreview` showing the final combination as text.
  - Buttons `_btnOK`, `_btnCancel`.

- **Startup behavior**:
  - Receives a `HotkeySettings` instance in the constructor.
  - `LoadSettings()`:
    - Sets checkbox states from the existing settings.
    - Tries to select the corresponding key in the combo box (fallback to first item).
    - Updates the preview.

- **Preview updates**:
  - Any change in checkboxes or key selection calls `UpdatePreview`:
    - Builds a list of active modifiers and selected key.
    - Displays them as `"Preview: Ctrl + Shift + E"`.

- **On OK / Form Closing**:
  - If dialog result is `OK`:
    - Validates:
      - At least one modifier is selected.
      - A key is selected.
    - If validation fails, shows a warning and cancels closing.
    - If valid:
      - Writes the new values into `Settings` (the `HotkeySettings` instance).
      - Uses `GetVirtualKey` to map key name back to VK code.

- `MainForm.OnChangeShortcut`:
  - Opens `HotkeySettingsForm` with current `_settings`.
  - If user clicks OK:
    - Updates `_settings` with `form.Settings`.
    - Saves via `Settings.Save`.
    - Calls `_hotkeyHandler.UpdateHotkey` to re-register the global hotkey.
    - Updates tray tooltip and shows info message.
  - If `UpdateHotkey` fails:
    - Shows error, reloads previous settings from disk.

---

### 6. Key Files & Responsibilities

- **`Program.cs`**
  - Entry point; configures WinForms app and runs `MainForm`.

- **`MainForm.cs`**
  - Invisible main window.
  - System tray integration (icon, context menu).
  - Owns `HotkeyHandler`.
  - Handles:
    - Hotkey registration.
    - Core correction flow.
    - Opening the settings dialog.
    - Clean disposal of resources.

- **`Settings.cs`**
  - Defines `HotkeySettings` (model).
  - Defines `Settings` (JSON persistence).

- **`HotkeySettingsForm.cs`**
  - Configuration UI for the hotkey.
  - Reads/writes `HotkeySettings` (via the `Settings` property).

- **`TextMapper.cs`**
  - Contains English→Arabic and Arabic→English mapping dictionaries.
  - Handles detection of Arabic text.
  - Implements the one-way and reverse mappings (including lam–alif special case).

- **`LanguageSwitch.cs`**
  - Contains `LanguageSwitch.SwitchLanguage()`.
  - Sends a Windows message to the foreground window to ask it to switch input language.

- **`HotkeyHandler.cs`**
  - Wraps Win32 `RegisterHotKey` / `UnregisterHotKey`.
  - Provides an event-based API for hotkey presses.
  - Uses a fixed hotkey id and checks `WM_HOTKEY` messages.

---

### 7. Building and Running

#### 7.1 Requirements

- **OS**: Windows 10 or later.
- **Runtime**: .NET (as configured in the project, currently `net8.0-windows`).
- **IDE**: Visual Studio, Rider, or VS Code with C# extension.

#### 7.2 Build

- Open the solution/project in your IDE.
- Select a **Debug** or **Release** configuration.
- Build the project:
  - Visual Studio: `Build → Build Solution`.
  - Or from terminal (if you know the `.csproj` path):

```bash
 dotnet build
```

#### 7.3 Run

- Run from IDE: `Debug → Start Debugging` or `Start Without Debugging`.
- Or from terminal:

```bash
 dotnet run
```

When running:

- You should **see the tray icon** appear.
- Right-click it to:
  - Change the shortcut.
  - Exit the app.

---

### 8. Contribution Guide

This section is for contributors who want to read, understand, and modify the code.

#### 8.1 Repo Structure (simplified)

- **Core C# files** (in project root):
  - `Program.cs`
  - `MainForm.cs`
  - `Settings.cs`
  - `HotkeySettingsForm.cs`
  - `TextMapper.cs`
  - `LanguageSwitch.cs`
  - `HotkeyHandler.cs`
- **Build artifacts**:
  - `obj/` – generated by the build (ignore in contributions).
  - `bin/` – compiled output (ignore in git).

#### 8.2 Coding Style & Conventions

- **Namespace**: `LangFlip` for all app code.
- **Visibility**:
  - Use `internal` for application classes (as already done).
- **Error handling**:
  - For user-facing errors (like failing to register a hotkey), show a `MessageBox`.
  - For background errors, log to debug output instead of crashing.
- **Interop**:
  - P/Invoke definitions (e.g., `DllImport`) sit close to the methods using them.
  - Keep `struct` definitions for Win32 input messages private within `MainForm` or `HotkeyHandler` as they are now.

#### 8.3 Typical Contribution Tasks

- **Change default hotkey**:
  - Edit default values in `HotkeySettings` (properties’ initial values).

- **Add/remove key mappings**:
  - Edit `EnglishToArabic` and `ArabicToEnglish` dictionaries in `TextMapper`.
  - Ensure mappings are **symmetric** when possible (A→X and X→A).
  - For special cases like lam‑alif, ensure the reverse logic in English conversion is correct.

- **Modify language-switch behavior**:
  - Adjust logic in `LanguageSwitch.SwitchLanguage`.
  - For example, you might target a specific layout instead of cycling.

- **Improve UI**:
  - Adjust layout and styling in `HotkeySettingsForm.InitializeComponent`.
  - Add additional instructions or validation messages.

#### 8.4 How to Safely Make Changes

- **Step 1 – Understand the flow**:
  - Follow the flow in section 3 (startup → hotkey → correction).

- **Step 2 – Make small, focused edits**:
  - Prefer touching **one component at a time**.
  - For example, if you update mapping logic, stay mostly in `TextMapper.cs`.

- **Step 3 – Test manually**:
  - Rebuild and run the app.
  - Test these scenarios:
    - Hotkey registers successfully (no error on startup).
    - Text typed in English but intended as Arabic is corrected correctly.
    - Text typed in Arabic but intended as English is converted back correctly.
    - Keyboard layout switches after correction.
    - Changing hotkey via tray menu:
      - New combination works.
      - Error is shown if hotkey is already in use (e.g., conflicts with existing system/global hotkey).

- **Step 4 – Keep behavior intuitive**:
  - The hotkey should always perform **“fix my last typed text”** in a predictable way.
  - Avoid long delays that make the app feel unresponsive.

#### 8.5 Adding New Features (Examples)

Here are some possible extension ideas and where to plug them in:

- **Support more language pairs**:
  - Extend `TextMapper` with additional dictionaries and update `ContainsArabic` logic (or generalize it).
  - Add configuration to select the current pair (e.g., Arabic–English vs something else).

- **Toggle behavior per app**:
  - Introduce logic in `MainForm.ExecuteCorrectionFlow` to detect the active process and skip or change behavior depending on app name.

- **Logging / diagnostics mode**:
  - Add an optional verbose log (e.g., to a file) to see:
    - Detected language.
    - Conversion results.
    - Any errors/exceptions.

---

### 9. Summary for New Contributors

- **Goal**: LangFlip fixes keyboard layout mistakes between Arabic and English using a global hotkey.
- **Core logic**:
  - `HotkeyHandler` catches hotkey → `MainForm` copies selection → `TextMapper` converts text → app pastes → `LanguageSwitch` switches OS layout.
- **Configuration**:
  - Users control the global hotkey via `HotkeySettingsForm` (stored in `%APPDATA%/LangFlip/settings.json`).
- **Where to start**:
  - Read `MainForm.cs` and `TextMapper.cs` first.
  - Then look at `Settings.cs`, `HotkeySettingsForm.cs`, and `HotkeyHandler.cs`.
