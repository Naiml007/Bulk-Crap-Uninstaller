/*
    Win11 theme helpers: dark title bar, Mica backdrop, and color tokens.
*/
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BulkCrapUninstaller.Functions.Tools
{
    public static class Win11Theme
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

        private const int DWMSBT_MAINWINDOW = 2;   // Mica
        private const int DWMWCP_ROUND = 2;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static bool IsDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                if (value is int i) return i == 0;
            }
            catch { }
            return false;
        }

        public static void ApplyBackdrop(Form form)
        {
            if (form == null || form.IsDisposed) return;
            if (!form.IsHandleCreated) return;

            int dark = IsDarkMode() ? 1 : 0;
            DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));

            int backdrop = DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(form.Handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));

            int corner = DWMWCP_ROUND;
            DwmSetWindowAttribute(form.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
        }

        // Color tokens
        public static Color Background => IsDarkMode() ? Color.FromArgb(32, 32, 32) : Color.FromArgb(243, 243, 243);
        public static Color Surface => IsDarkMode() ? Color.FromArgb(45, 45, 45) : Color.White;
        public static Color Text => IsDarkMode() ? Color.FromArgb(250, 250, 250) : Color.FromArgb(32, 32, 32);
        public static Color Accent => Color.FromArgb(0, 120, 212);
        public static Color Border => IsDarkMode() ? Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 220, 220);

        public static void ApplyToControl(Control control)
        {
            if (control == null) return;

            // Don't restyle third-party list controls
            if (control.GetType().FullName != null &&
                control.GetType().FullName.StartsWith("BrightIdeasSoftware", StringComparison.Ordinal))
                return;

            if (control is Button btn)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = Accent;
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderSize = 0;
                btn.Cursor = Cursors.Hand;
            }
            else if (control is TextBox || control is ComboBox)
            {
                control.BackColor = Surface;
                control.ForeColor = Text;
                if (control is TextBox tb) tb.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is CheckBox cb)
            {
                cb.FlatStyle = FlatStyle.Flat;
                cb.ForeColor = Text;
            }
            else if (control is Label lbl)
            {
                lbl.ForeColor = Text;
                lbl.BackColor = Color.Transparent;
            }
            else if (control is Panel || control is GroupBox || control is TabControl || control is SplitContainer)
            {
                control.BackColor = Background;
                control.ForeColor = Text;
            }
            else if (control is Form frm)
            {
                frm.BackColor = Background;
                frm.ForeColor = Text;
            }

            foreach (Control c in control.Controls)
                ApplyToControl(c);
        }

        public static void ApplyToForm(Form form)
        {
            if (form == null) return;
            ApplyBackdrop(form);
            ApplyToControl(form);
        }
    }
}
