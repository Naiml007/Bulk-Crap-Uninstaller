/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BulkCrapUninstaller.Theming;

namespace BulkCrapUninstaller.Controls
{
    /// <summary>
    /// Win11-style command bar: search box and quick filter chips.
    /// </summary>
    public class Win11CommandBar : UserControl
    {
        private readonly Dictionary<string, Win11Button> _chips = new();
        private readonly FlowLayoutPanel _chipPanel;
        private readonly Win11TextBox _searchBox;
        private bool _suppressSearchEvent;

        public event EventHandler<string> SearchTextChanged;
        public event EventHandler<string> FilterChipClicked;

        public Win11CommandBar()
        {
            Dock = DockStyle.Top;
            Height = 48;
            BackColor = Win11Theme.FillSolidSecondary;

            _searchBox = new Win11TextBox
            {
                Location = new Point(12, 12),
                Width = 280,
                PlaceholderText = "Search installed apps...",
                Font = Win11Theme.BodyFont,
                AccessibleName = "Search installed apps"
            };
            _searchBox.TextChanged += (s, e) =>
            {
                if (!_suppressSearchEvent)
                    SearchTextChanged?.Invoke(this, _searchBox.Text);
            };
            _searchBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    ClearSearch();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            Controls.Add(_searchBox);

            _chipPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 12, 0)
            };
            Controls.Add(_chipPanel);

            AddChip("All");
            AddChip("System");
            AddChip("Store");
            AddChip("Hidden");
            SetActiveChip("All");

            Win11Theme.ThemeChanged += OnThemeChanged;
        }

        public void SetSearchText(string text)
        {
            _suppressSearchEvent = true;
            _searchBox.Text = text ?? string.Empty;
            _suppressSearchEvent = false;
        }

        public void ClearSearch()
        {
            SetSearchText(string.Empty);
        }

        public void FocusSearch()
        {
            _searchBox.Focus();
        }

        public string ActiveChip { get; private set; } = "All";

        public void SetActiveChip(string text)
        {
            ActiveChip = text;
            foreach (var chip in _chips)
                chip.Value.IsAccent = chip.Key == text;
        }

        private void AddChip(string text)
        {
            var chip = new Win11Button
            {
                Text = text,
                Width = TextRenderer.MeasureText(text, Win11Theme.BodyFont).Width + 24,
                Height = 32,
                Margin = new Padding(4, 0, 0, 0),
                Font = Win11Theme.BodyFont,
                CornerRadius = 16
            };
            chip.Click += (s, e) =>
            {
                SetActiveChip(text);
                FilterChipClicked?.Invoke(this, text);
            };
            _chips[text] = chip;
            _chipPanel.Controls.Add(chip);
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            BackColor = Win11Theme.FillSolidSecondary;
            foreach (Control chip in _chipPanel.Controls)
                chip.Invalidate();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Win11Theme.Stroke, 1f);
            e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Win11Theme.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
