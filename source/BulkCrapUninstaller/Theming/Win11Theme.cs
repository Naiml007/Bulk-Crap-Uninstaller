/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Drawing;
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
