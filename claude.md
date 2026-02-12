# Claude Code Guidelines for LangFlip

This document contains rules and best practices for working on the LangFlip project.

---

## Project Context

LangFlip is a Windows tray application that fixes text typed in the wrong keyboard layout (Arabic ↔ English) using a global hotkey. The app runs in the background with minimal UI.

---

## C# Best Practices

### Naming Conventions

- **Classes, methods, properties**: Use `PascalCase`
  - `HotkeySettings`, `ExecuteCorrectionFlow()`, `VirtualKey`
- **Private fields**: Use `_camelCase` with underscore prefix
  - `_hotkeyHandler`, `_chkCtrl`, `_lblPreview`
- **Local variables, parameters**: Use `camelCase`
  - `clipboardText`, `convertToArabic`, `isValid`
- **Constants**: Use `UPPER_CASE` or `PascalCase` depending on visibility
  - `HOTKEY_ID`, `MOD_CONTROL`

### Code Organization

- Keep related functionality together in single files
- Use `internal` visibility for application classes (this is a standalone app, not a library)
- Place P/Invoke declarations close to the methods using them
- Keep Win32 structs and constants private within the class that uses them

### Error Handling

- **User-facing errors**: Show `MessageBox` for critical failures
  - Example: Hotkey registration failure, invalid configuration
- **Background errors**: Log to debug output using `Debug.WriteLine()`, don't crash
  - Example: Clipboard access failures during correction flow
- Use try-catch blocks around operations that can fail silently
- Validate user input in UI forms before processing

### Resource Management

- Implement `IDisposable` for classes managing unmanaged resources
- Always dispose of Win32 handles, icons, and other system resources
- Use `using` statements or explicit `Dispose()` calls
- Unregister hotkeys in `Dispose()` to avoid resource leaks

### LINQ and Modern C#

- Use LINQ where it improves readability
- Prefer modern C# features (pattern matching, null-coalescing, string interpolation)
- Use nullable reference types to prevent null reference exceptions
- Leverage expression-bodied members for simple properties/methods

---

## Project-Specific Rules

### Architecture Principles

1. **Single Responsibility**: Each class has one clear purpose
   - `TextMapper`: Character conversion only
   - `HotkeyHandler`: Hotkey registration/events only
   - `Settings`: Persistence only
   - `MainForm`: Orchestration and UI management

2. **Keep it Simple**: This is a small utility app
   - No over-engineering or premature abstractions
   - Direct, straightforward implementations
   - Avoid adding frameworks or complex patterns unless necessary

3. **Minimal UI**: App runs in background
   - No visible main window
   - Tray icon with context menu only
   - Settings dialog appears only when requested

### Core Flow (Don't Break This)

The correction flow must follow this sequence:
1. Copy selection (`Ctrl+A` → `Ctrl+C`)
2. Detect language (Arabic vs English)
3. Convert text using `TextMapper`
4. Paste converted text (`Ctrl+V`)
5. Switch OS input language

**Never** modify this flow without understanding the complete impact.

### Settings and Configuration

- Settings stored in `%APPDATA%/LangFlip/settings.json`
- Always provide sensible defaults (Shift + Win + E)
- Use `HotkeySettings.Default` when loading fails
- Save settings after user confirms changes (not on every keystroke)
- Swallow exceptions during save (don't crash on write failure)

### Hotkey Management

- Use a single global hotkey (not multiple)
- Default: `Shift + Win + E`
- Always require at least one modifier key (prevent accidental triggers)
- Validate hotkey before registration
- Handle registration failures gracefully (show error to user)
- Unregister old hotkey before registering new one
- Update tray tooltip to show current hotkey

### Text Conversion Rules

- Mappings in `TextMapper` must be **symmetric** when possible
  - If `'a'` → `'ش'`, then `'ش'` → `'a'`
- Handle special cases explicitly:
  - **Lam-alif**: `'b'/'B'` → `'ل' + 'ا'` (two characters)
  - Reverse: `'ل' + 'ا'` → `'b'`
- Preserve characters not in the mapping (numbers, punctuation, spaces)
- Language detection: Check for Arabic Unicode range (`0x0600`–`0x06FF`)

### UI Guidelines

- Keep forms minimal and focused
- Validate input before closing dialogs
- Show preview of settings changes
- Use descriptive labels and tooltips
- Don't show success messages unless user expects confirmation
- Always show error messages for failures that affect functionality

### Clipboard Operations

- Use retry logic with delays (clipboard access can fail)
- Read clipboard before copy to detect if selection changed
- Clear clipboard after use only if necessary
- Handle clipboard access failures gracefully
- Add small delays (`Thread.Sleep`) between clipboard operations

### Win32 Interop

- Use `DllImport` for required Win32 APIs
- Keep P/Invoke signatures close to usage
- Use appropriate character sets (`CharSet.Unicode` for string parameters)
- Handle Win32 errors (check return values, use `Marshal.GetLastWin32Error()`)
- Keep Win32 constants and enums private to the class using them

---

## Code Quality Standards

### Before Committing Changes

1. **Build succeeds** with no warnings
2. **Manually test** the following:
   - App starts and tray icon appears
   - Hotkey triggers correction
   - English → Arabic conversion works
   - Arabic → English conversion works
   - Keyboard layout switches after correction
   - Changing hotkey via tray menu works
   - Invalid hotkey shows appropriate error

3. **Edge cases to consider**:
   - Empty selection
   - Very long text (>10,000 characters)
   - Mixed Arabic/English text
   - Special characters and numbers
   - Hotkey conflicts with other applications

### Adding New Features

- **Read CONTRIBUTING.md first** to understand the architecture
- Start with `MainForm.cs` and `TextMapper.cs` to understand core logic
- Make **small, focused changes** (one component at a time)
- Don't break existing functionality
- Keep the app responsive (avoid long-running operations on UI thread)
- Document complex logic with comments

### Performance Considerations

- Keep hotkey response fast (<500ms total)
- Don't perform heavy operations in UI event handlers
- Use appropriate delays for clipboard operations (20-200ms)
- Avoid unnecessary allocations in hot paths

---

## Specific Don'ts

1. **Don't** add dependencies or NuGet packages without good reason
2. **Don't** make the app more complex than necessary
3. **Don't** add features that aren't directly related to fixing keyboard layout mistakes
4. **Don't** show unnecessary UI dialogs or notifications
5. **Don't** crash on non-critical errors (log instead)
6. **Don't** modify the core correction flow without thorough testing
7. **Don't** break backwards compatibility with existing settings files
8. **Don't** use async/await unless absolutely necessary (this is a simple synchronous app)
9. **Don't** add configuration options for things that should "just work"
10. **Don't** ignore Win32 API errors (always check return values)

---

## Testing Checklist

When modifying code, manually verify:

- [ ] App starts without errors
- [ ] Tray icon displays with correct tooltip
- [ ] Default hotkey works on fresh install
- [ ] Custom hotkey can be set and persists
- [ ] English text converts to Arabic correctly
- [ ] Arabic text converts to English correctly
- [ ] Lam-alif special case works both directions
- [ ] Keyboard layout switches after correction
- [ ] Error shown when hotkey conflicts with system hotkey
- [ ] App exits cleanly from tray menu
- [ ] Settings survive app restart
- [ ] App handles empty clipboard gracefully
- [ ] App handles clipboard access failures

---

## Common Pitfall Areas

### Clipboard Access
- Can fail due to other apps locking it
- Needs retry logic with delays
- Can't assume clipboard contains text

### Hotkey Registration
- Can fail if hotkey already in use
- Must unregister before app exits
- Can't register hotkey without modifier keys

### SendKeys
- Timing-sensitive (needs delays)
- Can fail if foreground window changes
- Different apps handle SendKeys differently

### Language Switching
- Only requests switch (doesn't guarantee it)
- Target app must support language switching
- Some apps ignore the switch request

---

## Files and Their Responsibilities

| File | Purpose | Modify When... |
|------|---------|----------------|
| `Program.cs` | App entry point | Changing startup behavior |
| `MainForm.cs` | Main orchestration, tray UI | Adding menu items, changing correction flow |
| `Settings.cs` | Settings model and persistence | Adding new settings, changing storage format |
| `HotkeySettingsForm.cs` | Hotkey configuration UI | Changing hotkey selection UI |
| `TextMapper.cs` | Character conversion logic | Adding/changing language mappings |
| `LanguageSwitch.cs` | OS language switching | Changing how language switch works |
| `HotkeyHandler.cs` | Win32 hotkey registration | Changing hotkey behavior |

---

## When in Doubt

1. **Read the existing code** to understand current patterns
2. **Follow the established style** (consistency over personal preference)
3. **Keep it simple** (this is a small utility, not enterprise software)
4. **Test manually** before committing
5. **Ask questions** rather than making assumptions about requirements

---

## Summary

LangFlip is a **focused utility** that does one thing well: fixing keyboard layout mistakes. Keep changes aligned with this goal, maintain simplicity, and ensure reliability through thorough manual testing.
