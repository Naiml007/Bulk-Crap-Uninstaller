# BCUninstaller — Windows 11 UI/UX Redesign Plan

> Reference: Windows 11 Fluent Design / WinUI 3 component language  
> Current stack: WinForms (.NET 8)  
> Scope: visual redesign within WinForms using custom-drawn controls + Win11 theming where possible

---

## 1. Design tokens (Windows 11 system values)

| Token | Light | Dark |
|---|---|---|
| `Fill/Solid/Base` | `#FFFFFF` | `#202020` |
| `Fill/Solid/Secondary` | `#F3F3F3` | `#2C2C2C` |
| `Fill/Solid/Tertiary` | `#EBEBEB` | `#383838` |
| `Accent/Default` | `#0067C0` | `#60CDFF` |
| `Accent/Hover` | `#1A6EC7` | `#79D2FF` |
| `Accent/Pressed` | `#005BA3` | `#45C0F5` |
| `Text/Primary` | `#1A1A1A` | `#FFFFFF` |
| `Text/Secondary` | `#5D5D5D` | `#9D9D9D` |
| `Text/Disabled` | `#A5A5A5` | `#5C5C5C` |
| `Stroke/Default` | `rgba(0,0,0,0.0803)` | `rgba(255,255,255,0.0837)` |
| `ControlCornerRadius` | 4px | 4px |
| `OverlayCornerRadius` | 8px | 8px |
| `CardCornerRadius` | 4px | 4px |
| `Elevation/Shadow/Card` | `0 2px 4px rgba(0,0,0,0.04)` | same |
| `Elevation/Shadow/Flyout` | `0 8px 16px rgba(0,0,0,0.14)` | same |
| `Font/Body` | Segoe UI Variable, 14px | same |
| `Font/BodyStrong` | Segoe UI Variable Semibold, 14px | same |
| `Font/Subtitle` | Segoe UI Variable Semibold, 20px | same |
| `Font/Title` | Segoe UI Variable Light, 28px | same |
| `Font/Caption` | Segoe UI Variable, 12px | same |
| `Mica/Tint` | `rgba(243,243,243,0.5)` | `rgba(32,32,32,0.5)` |

---

## 2. Window chrome

**Current:** Legacy WinForms chrome (old title bar, no snap layout, no dark mode).

**Target:**
- Call `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE)` to enable system dark title bar.
- Call `DwmSetWindowAttribute(DWMWA_SYSTEMBACKDROP_TYPE, DWMSBT_MAINWINDOW)` for Mica background (Win11 22H2+).
- Remove custom `FormBorderStyle` overrides; use `Sizable` with snap layouts.
- Title bar buttons (min/max/close) inherit system rendering — no custom drawing needed.
- Window background: draw Mica tint via `WM_ERASEBKGND` override when Mica is unavailable.

---

## 3. Screen-by-screen redesign

### 3.1 MainWindow

**Current layout:** Toolbar → ListView (apps) → split sidebar (PropertiesSidebar)

```
┌──────────────────────────────────────────────────────┐
│ [Search...____________________] [Filter▼] [Refresh]   │  ← CommandBar (48px, Mica)
├──────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────┐ ┌──────────────┐ │
│ │  App list (DataGrid style)      │ │  Properties  │ │
│ │  ☑ Name  Publisher  Size  Date  │ │  sidebar     │ │
│ │  ─────────────────────────────  │ │  (collapsible│ │
│ │  □ Adobe Acrobat   Adobe  450MB │ │   320px)     │ │
│ │  □ ...                          │ │              │ │
│ └─────────────────────────────────┘ └──────────────┘ │
├──────────────────────────────────────────────────────┤
│ 72 apps  │  Selected: 3  │  [Uninstall Selected ▼]   │  ← StatusBar (40px)
└──────────────────────────────────────────────────────┘
```

**Win11 changes:**
- **CommandBar**: `Fill/Solid/Secondary` bg, 1px bottom stroke. Search box → `AutoSuggestBox` style (rounded pill, 36px height, Segoe Fluent Icons `Search` glyph).
- **Filter chips**: pill-shaped toggle buttons (`ControlCornerRadius 16px`) for quick filters (System, Store, Hidden). Active state: `Accent/Default` fill + white text.
- **App list**: Replace `ListView` with owner-drawn `DataGridView`. Rows 48px tall. Alternating rows: `Fill/Solid/Base` / `Fill/Solid/Secondary`. Selected row: `Accent/Default` at 15% opacity. Hover: `Fill/Solid/Tertiary`. Column headers: `Font/Caption`, uppercase, `Text/Secondary` colour.
- **Rating stars**: Custom render inline in the list (keep existing logic, re-skin to filled/outline SVG-style glyphs).
- **Sidebar toggle**: Chevron button on the splitter edge, `OverlayCornerRadius`, slides in/out with 200ms ease-out.
- **Uninstall button**: Split button — left half runs default action, right half opens dropdown. `Accent/Default` fill, white text, `ControlCornerRadius`.

---

### 3.2 PropertiesSidebar

**Current:** TabControl with property labels.

**Target:**
```
┌──────────────────────┐
│ App Icon (48px)      │
│ App Name  Subtitle   │
│ ─────────────────── │
│ Publisher    Adobe   │
│ Version      24.0    │
│ Install date ...     │
│ Size         450 MB  │
│ Location     [open↗] │
│ ─────────────────── │
│ [Uninstall] [Modify] │
└──────────────────────┘
```
- Remove `TabControl` — single scrollable pane.
- Section dividers: 1px `Stroke/Default` lines.
- Icon: 48×48, `CardCornerRadius` clip, `Elevation/Shadow/Card`.
- Property rows: label `Text/Secondary` 12px, value `Text/Primary` 14px, two-column grid.
- Action buttons: standard `Button` style, `ControlCornerRadius`, full width.

---

### 3.3 SettingsWindow

**Current:** Tab-based settings form.

**Target:** NavigationView-style (vertical nav, like Windows Settings).

```
┌──────────────────────────────────────┐
│ ← Settings                           │  ← page title
├──────────┬───────────────────────────┤
│ 🎨 Appearance │                      │
│ 🗑 Uninstall  │  Appearance          │
│ 🔍 Scan       │  ─────────────────── │
│ ⚙ Advanced   │  Theme  [Light ▼]    │
│               │  Language [English▼] │
│               │  ...                 │
└──────────┴───────────────────────────┘
```
- Left nav: 240px, `Fill/Solid/Secondary` bg. Items 40px tall, `ControlCornerRadius`, left accent bar on active.
- Content area: `Fill/Solid/Base`, padded 24px.
- Settings rows: each option is a `SettingsCard` — white card, 72px tall, icon + label + control, `CardCornerRadius`, `Elevation/Shadow/Card`.

---

### 3.4 BeginUninstallTaskWizard

**Current:** Step-based wizard dialog.

**Target:** ContentDialog style (modal overlay, 480px wide, `OverlayCornerRadius`).
- Scrim: `rgba(0,0,0,0.3)` over main window.
- Header: app icon(s) + "Uninstall X apps?" title.
- Body: scrollable checklist of selected apps.
- Footer: `[Cancel]` (secondary) + `[Uninstall]` (accent/primary) — right-aligned.

---

### 3.5 UninstallProgressWindow

**Current:** Progress bar + log textarea.

**Target:**
```
┌────────────────────────────────────────┐
│  Uninstalling Adobe Acrobat...         │
│  ████████████░░░░░░░░░  3 of 5         │
│                                        │
│  ▼ Details                             │
│  ┌──────────────────────────────────┐  │
│  │  log output (monospace 12px)     │  │
│  └──────────────────────────────────┘  │
│                     [Cancel] [Done ✓]  │
└────────────────────────────────────────┘
```
- `ProgressBar`: custom-drawn, `Accent/Default` fill, 4px `ControlCornerRadius`, animated shimmer on indeterminate.
- Collapsible "Details" expander (chevron toggle).
- Log area: `Fill/Solid/Tertiary` bg, `Consolas` font, auto-scroll.

---

### 3.6 JunkRemoveWindow

**Current:** ListView of junk items with checkboxes.

**Target:**
- Group items by type (Registry, Files, Folders, Shortcuts) with collapsible `Expander` headers.
- Each group header shows count badge (pill, `Accent/Default` bg).
- Items: 40px rows, checkbox + icon + path + size right-aligned.
- Footer: total size to free + `[Remove Selected]` accent button.

---

### 3.7 FirstStartBox / AboutBox

**Current:** Plain message dialogs.

**Target:** ContentDialog with logo, version, links styled as `HyperlinkButton` (underline on hover, `Accent` colour).

---

## 4. Custom control inventory

| Control needed | Replaces | Complexity |
|---|---|---|
| `Win11Button` | `Button` | Low — border radius + colour tokens |
| `Win11SplitButton` | custom | Medium — dropdown arrow, flyout |
| `Win11TextBox` | `TextBox` | Low — radius, focus stroke |
| `Win11SearchBox` | `TextBox` | Low — search icon, clear button |
| `Win11ComboBox` | `ComboBox` | Medium — custom dropdown rendering |
| `Win11CheckBox` | `CheckBox` | Low — Fluent check glyph |
| `Win11ProgressBar` | `ProgressBar` | Medium — shimmer animation |
| `Win11ListView` | `DataGridView` | High — owner-draw rows, selection |
| `Win11NavigationView` | `TabControl` | High — left nav, animated indicator |
| `Win11Expander` | none | Medium — animated chevron collapse |
| `Win11SettingsCard` | `GroupBox` | Low — card shadow, icon slot |
| `Win11ContentDialog` | `Form` modal | Medium — scrim, `OverlayCornerRadius` |
| `Win11Tooltip` | `ToolTip` | Low — radius, shadow |

---

## 5. Implementation order (phased)

### Phase 1 — Tokens + Chrome (no layout change)
1. Add `Win11Theme.cs` — static colour/font/radius token class, reads `SystemInformation.HighContrast`, auto-switches light/dark based on `SystemEvents.UserPreferenceChanged`.
2. Apply Mica / dark title bar via P/Invoke in `MainWindow`.
3. Subclass `Button`, `TextBox`, `CheckBox`, `ComboBox` with token-based `OnPaint` overrides.
4. Apply to all existing forms — no layout changes yet.

### Phase 2 — MainWindow layout
5. Replace `ListView` with owner-drawn `DataGridView` subclass (`Win11ListView`).
6. Redesign CommandBar (search box, filter chips, action buttons).
7. Animate sidebar collapse.

### Phase 3 — Dialogs
8. `Win11ContentDialog` base class — scrim + modal hosting.
9. Migrate `BeginUninstallTaskWizard`, `UninstallConfirmation`, `JunkRemoveWindow` to it.
10. `UninstallProgressWindow` — shimmer progress, collapsible log.

### Phase 4 — Settings
11. `Win11NavigationView` left-nav shell.
12. `Win11SettingsCard` rows.
13. Migrate `SettingsWindow` pages.

---

## 6. Files to create

```
source/BulkCrapUninstaller/
  Theming/
    Win11Theme.cs           ← token constants + dark-mode detection
    Win11DwmHelper.cs       ← P/Invoke: Mica, dark title bar
  Controls/
    Win11Button.cs
    Win11SplitButton.cs
    Win11TextBox.cs
    Win11SearchBox.cs
    Win11ComboBox.cs
    Win11CheckBox.cs
    Win11ProgressBar.cs
    Win11ListView.cs
    Win11NavigationView.cs
    Win11Expander.cs
    Win11SettingsCard.cs
    Win11ContentDialog.cs
    Win11Tooltip.cs
  Controls/Renderers/
    FluentScrollBarRenderer.cs
    FluentToolStripRenderer.cs
```

---

## 7. What this plan does NOT cover

- Replacing WinForms with WinUI 3 / MAUI (different project — requires full rewrite).
- Animation framework (WinForms has no built-in animation; `System.Windows.Forms.Timer` is used for shimmer/collapse — acceptable for Phase 1–2).
- Mica on Windows 10 (falls back to solid `Fill/Solid/Base` — handled by `Win11DwmHelper`).
- RTL layout (existing app doesn't support it; out of scope).
