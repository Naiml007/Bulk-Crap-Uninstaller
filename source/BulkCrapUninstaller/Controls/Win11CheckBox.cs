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
    public class Win11CheckBox : CheckBox
    {
        public Win11CheckBox()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer,
                true);
            Appearance = Appearance.Normal;
            AutoSize = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Clear background
            g.Clear(Parent?.BackColor ?? SystemColors.Control);

            const int boxSize = 16;
            int x = 0;
            int y = (Height - boxSize) / 2;
            var boxRect = new Rectangle(x, y, boxSize - 1, boxSize - 1);

            const int r = 4;
            using (var path = RoundedRect(boxRect, r))
            {
                if (Checked)
                {
                    using (var brush = new SolidBrush(Win11Theme.AccentDefault))
                        g.FillPath(brush, path);

                    using var checkFont = new Font("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point);
                    TextRenderer.DrawText(g, "\u2713", checkFont, boxRect, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                }
                else
                {
                    using (var brush = new SolidBrush(Win11Theme.FillSolidBase))
                        g.FillPath(brush, path);

                    using var pen = new Pen(Win11Theme.Stroke, 1f);
                    g.DrawPath(pen, path);
                }
            }

            // Draw label text to the right of the box
            var textX = boxSize + 6;
            var textY = (Height - Font.Height) / 2;
            TextRenderer.DrawText(g, Text, Font,
                new Point(textX, textY), Win11Theme.TextPrimary);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
