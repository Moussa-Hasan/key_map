## LangFlip

LangFlip is a small Windows tray app that fixes text you typed with the **wrong keyboard layout** (English ↔ Arabic) using a single global shortcut.

It does **keyboard layout mapping only** (QWERTY ↔ Arabic 101) – no translation, no cloud.

---

### What it does

- **Global hotkey**: **Ctrl + Shift + Q**
- When you press it, LangFlip:
  1. Selects all text in the active control.
  2. Copies the text to the clipboard.
  3. Detects direction:
     - If it finds Arabic characters, it maps **Arabic → English**.
     - Otherwise it maps **English → Arabic**.
  4. Replaces the clipboard content with the converted text.
  5. Pastes the converted text back in place.
  6. Sends **Alt+Shift** once to let Windows flip the active keyboard layout too.

---

### How to use

1. Open any text field (Notepad, editor, browser input, chat, etc.).
2. Type as usual. If you realize you typed with the wrong layout:
3. Press **Ctrl + Shift + Q**.
4. The text flips between English and Arabic, and Windows usually toggles the active keyboard layout.

If clipboard access fails or the app cannot read the selection, it simply returns without showing a dialog.

---

### Current limitations

- **Only English ↔ Arabic (Arabic 101 layout)** is supported.
- **No per‑app behavior** – any focused control that allows `Ctrl+A`/`Ctrl+C`/`Ctrl+V` and clipboard access will be affected.
- **Pure character mapping** – it does not understand words or grammar, it only remaps keys.
- **Some apps may block automation**: if an application blocks simulated key presses or clipboard access, LangFlip will not work there.

---

### Run (dev)

- **Requirement**: .NET 8 SDK on Windows.
- From the repo root:

```powershell
dotnet build LangFlip.csproj
.\bin\Debug\net8.0-windows\LangFlip.exe
```

You should see a tray icon with the tooltip:

> LangFlip active (Ctrl+Shift+Q)

Right‑click the icon → **Exit** to quit the app.

---

### Build a portable exe

To build a trimmed portable executable for sharing:

```powershell
dotnet publish LangFlip.csproj -c Release -r win-x64 ^
  --self-contained false ^
  -p:PublishSingleFile=true ^
  -p:IncludeAllContentForSelfExtract=true
```

The main binary will land under:

- `bin\Release\net8.0-windows\win-x64\LangFlip.exe`

You can drop `LangFlip.exe` (and optionally `LangFlip.ico`) onto any .NET 8 capable Windows machine and run it directly.

---

### Install / Run for users

- Download or copy `LangFlip.exe` (and optionally `LangFlip.ico`) to any folder.
- Double‑click `LangFlip.exe`.
- Look for the tray icon; hover it to confirm the tooltip:

> LangFlip active (Ctrl+Shift+Q)

To quit, right‑click the tray icon → **Exit**.

---

### Troubleshooting

- **Nothing happens when I press the hotkey**:
  - Make sure LangFlip is running and visible in the tray.
  - Check that another app is not already using **Ctrl + Shift + Q** as a global shortcut.
- **It works in some apps but not others**:
  - Some apps block simulated key presses or clipboard access; LangFlip cannot work around that.
- **My keyboard layout is not Arabic 101**:
  - The mapping assumes Arabic 101; other variants may not line up correctly.

---

### License

Licensed under the MIT License.