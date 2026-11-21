/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BulkCrapUninstaller.Theming;

namespace BulkCrapUninstaller.Controls
{
    public class Win11TextBox : TextBox
    {
        private bool _hasFocus;

        public Win11TextBox()
        {
            BorderStyle = BorderStyle.None;
            BackColor = Win11Theme.FillSolidBase;
            ForeColor = Win11Theme.TextPrimary;
            Win11Theme.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(object sender, System.EventArgs e)
        {
            BackColor = Win11Theme.FillSolidBase;
            ForeColor = Win11Theme.TextPrimary;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Win11Theme.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }

        protected override void OnGotFocus(System.EventArgs e)
        {
            _hasFocus = true;
            Invalidate();
            Parent?.Invalidate(Bounds, false);
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(System.EventArgs e)
        {
            _hasFocus = false;
            Invalidate();
            Parent?.Invalidate(Bounds, false);
            base.OnLostFocus(e);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            // WM_PAINT = 0x000F, WM_NCPAINT = 0x0085
            if (m.Msg == 0x000F || m.Msg == 0x0085)
                DrawBorder();
        }

        private void DrawBorder()
        {
            using var g = CreateGraphics();
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var pen = new Pen(Win11Theme.Stroke, 1f))
                g.DrawRectangle(pen, rect);

            if (_hasFocus)
            {
                // 2px accent line at the bottom
                using var pen = new Pen(Win11Theme.AccentDefault, 2f);
                g.DrawLine(pen, 1, Height - 1, Width - 2, Height - 1);
            }
        }
    }
}
