/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BulkCrapUninstaller.Theming;

namespace BulkCrapUninstaller.Controls
{
    public class Win11Button : Button
    {
        private bool _isAccent;
        private bool _isHovered;
        private bool _isPressed;
        private int _cornerRadius = Win11Theme.ControlCornerRadius;

        public Win11Button()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
        }

        public bool IsAccent
        {
            get => _isAccent;
            set { _isAccent = value; Invalidate(); }
        }

        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

        protected override void OnMouseEnter(System.EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            _isHovered = false;
            _isPressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _isPressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _isPressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color fill;
            if (IsAccent)
                fill = _isPressed ? Win11Theme.AccentPressed : _isHovered ? Win11Theme.AccentHover : Win11Theme.AccentDefault;
            else
                fill = _isPressed || _isHovered ? Win11Theme.FillSolidTertiary : Win11Theme.FillSolidSecondary;

            int r = Math.Min(CornerRadius, Math.Min(Width, Height) / 2);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var path = RoundedRect(rect, r))
            {
                g.SetClip(path);
                using (var brush = new SolidBrush(fill))
                    g.FillPath(brush, path);

                g.ResetClip();

                using (var pen = new Pen(Win11Theme.Stroke, 1f))
                    g.DrawPath(pen, path);
            }

            Color textColor = IsAccent ? Color.White : Win11Theme.TextPrimary;
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
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
