/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BulkCrapUninstaller.Theming
{
    internal static class Win11Theme
    {
        private static bool _cachedIsDarkMode = ReadIsDarkMode();

        static Win11Theme()
        {
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General)
                return;
            var current = ReadIsDarkMode();
            if (current != _cachedIsDarkMode)
            {
                _cachedIsDarkMode = current;
                ThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        private static bool ReadIsDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int v)
                    return v == 0;
            }
            catch { /* ignore */ }
            return false;
        }

        public static event EventHandler ThemeChanged;

        public static bool IsDarkMode => _cachedIsDarkMode;

        public static int ControlCornerRadius => 4;

        // ----- Fill colors -----
        public static Color FillSolidBase =>
            IsDarkMode ? Color.FromArgb(0x20, 0x20, 0x20) : Color.FromArgb(0xF3, 0xF3, 0xF3);

        public static Color FillSolidSecondary =>
            IsDarkMode ? Color.FromArgb(0x2C, 0x2C, 0x2C) : Color.FromArgb(0xEB, 0xEB, 0xEB);

        public static Color FillSolidTertiary =>
            IsDarkMode ? Color.FromArgb(0x38, 0x38, 0x38) : Color.FromArgb(0xE0, 0xE0, 0xE0);

        // ----- Accent colors -----
        public static Color AccentDefault =>
            IsDarkMode ? Color.FromArgb(0x60, 0xCD, 0xFF) : Color.FromArgb(0x00, 0x67, 0xC0);

        public static Color AccentHover =>
            IsDarkMode ? Color.FromArgb(0x79, 0xD2, 0xFF) : Color.FromArgb(0x1A, 0x6E, 0xC7);

        public static Color AccentPressed =>
            IsDarkMode ? Color.FromArgb(0x45, 0xC0, 0xF5) : Color.FromArgb(0x00, 0x5B, 0xA3);

        // Always white text on accent fill
        public static Color AccentButtonText => Color.White;

        /// <summary>
        /// Accent blended over the base fill, used for selected list rows.
        /// </summary>
        public static Color SelectionFill
        {
            get
            {
                var accent = AccentDefault;
                var background = FillSolidBase;
                const float amount = 0.15f;
                return Color.FromArgb(
                    (int)(accent.R * amount + background.R * (1 - amount)),
                    (int)(accent.G * amount + background.G * (1 - amount)),
                    (int)(accent.B * amount + background.B * (1 - amount)));
            }
        }

        // ----- Text colors -----
        public static Color TextPrimary =>
            IsDarkMode ? Color.FromArgb(0xFF, 0xFF, 0xFF) : Color.FromArgb(0x1A, 0x1A, 0x1A);

        public static Color TextSecondary =>
            IsDarkMode ? Color.FromArgb(0x9D, 0x9D, 0x9D) : Color.FromArgb(0x5D, 0x5D, 0x5D);

        public static Color TextDisabled =>
            IsDarkMode ? Color.FromArgb(0x5C, 0x5C, 0x5C) : Color.FromArgb(0xA5, 0xA5, 0xA5);

        // ----- Stroke -----
        public static Color Stroke =>
            IsDarkMode ? Color.FromArgb(21, 255, 255, 255) : Color.FromArgb(20, 0, 0, 0);

        // ----- Font -----
        public static Font BodyFont { get; } = CreateBodyFont();

        /// <summary>
        /// ToolStrip renderer matching the current theme.
        /// </summary>
        public static ToolStripRenderer CreateToolStripRenderer()
        {
            return new ToolStripProfessionalRenderer(new Win11ColorTable()) { RoundedEdges = true };
        }

        private sealed class Win11ColorTable : ProfessionalColorTable
        {
            private static Color Surface => FillSolidSecondary;
            private static Color Hover => FillSolidTertiary;
            private static Color Pressed => FillSolidBase;

            public override Color ToolStripGradientBegin => Surface;
            public override Color ToolStripGradientMiddle => Surface;
            public override Color ToolStripGradientEnd => Surface;
            public override Color ToolStripBorder => Hover;

            public override Color MenuStripGradientBegin => Surface;
            public override Color MenuStripGradientEnd => Surface;
            public override Color StatusStripGradientBegin => Surface;
            public override Color StatusStripGradientEnd => Surface;

            public override Color MenuItemSelected => Hover;
            public override Color MenuItemSelectedGradientBegin => Hover;
            public override Color MenuItemSelectedGradientEnd => Hover;
            public override Color MenuItemBorder => Hover;
            public override Color MenuBorder => Hover;

            public override Color ButtonSelectedHighlight => Hover;
            public override Color ButtonSelectedHighlightBorder => Hover;
            public override Color ButtonSelectedGradientBegin => Hover;
            public override Color ButtonSelectedGradientMiddle => Hover;
            public override Color ButtonSelectedGradientEnd => Hover;
            public override Color ButtonPressedHighlight => Pressed;
            public override Color ButtonPressedGradientBegin => Pressed;
            public override Color ButtonPressedGradientMiddle => Pressed;
            public override Color ButtonPressedGradientEnd => Pressed;
            public override Color ButtonCheckedGradientBegin => Hover;
            public override Color ButtonCheckedGradientMiddle => Hover;
            public override Color ButtonCheckedGradientEnd => Hover;

            public override Color CheckBackground => Surface;
            public override Color CheckSelectedBackground => Hover;
            public override Color CheckPressedBackground => Pressed;

            public override Color ToolStripDropDownBackground => Surface;
            public override Color ImageMarginGradientBegin => Surface;
            public override Color ImageMarginGradientMiddle => Surface;
            public override Color ImageMarginGradientEnd => Surface;
            public override Color ImageMarginRevealedGradientBegin => Surface;
            public override Color ImageMarginRevealedGradientMiddle => Surface;
            public override Color ImageMarginRevealedGradientEnd => Surface;

            public override Color OverflowButtonGradientBegin => Surface;
            public override Color OverflowButtonGradientMiddle => Surface;
            public override Color OverflowButtonGradientEnd => Surface;

            public override Color SeparatorDark => Hover;
            public override Color SeparatorLight => Hover;
        }


        private static Font CreateBodyFont()
        {
            try
            {
                return new Font("Segoe UI Variable", 9f, FontStyle.Regular, GraphicsUnit.Point);
            }
            catch
            {
                return new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
            }
        }
    }
}
