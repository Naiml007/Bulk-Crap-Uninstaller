/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BulkCrapUninstaller.Theming
{
    internal static class Win11DwmHelper
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMSBT_MAINWINDOW = 2;

        public static void ApplyTheme(Form form)
        {
            try
            {
                int dark = Win11Theme.IsDarkMode ? 1 : 0;
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
            }
            catch { /* DWM unavailable on older Windows; ignore */ }

            if (Environment.OSVersion.Version.Build >= 22621)
            {
                try
                {
                    int backdrop = DWMSBT_MAINWINDOW;
                    DwmSetWindowAttribute(form.Handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
                }
                catch { /* Mica backdrop not supported; ignore */ }
            }
        }
    }
}
