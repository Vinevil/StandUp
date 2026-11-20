using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace StandUp {
    public sealed class UiTheme {
        public readonly int Index;
        public readonly Color Background, Surface, Border, Text, Muted, Accent, AccentInk, Hover, Alert;
        public static readonly UiTheme White = new UiTheme(0, "#F9FAFB", "#EFF1F4", "#D8DDE4", "#222A36", "#536172", "#293648", "#FFFFFF", "#E3E8EE", "#A24735");
        public static readonly UiTheme Black = new UiTheme(1, "#191B20", "#262930", "#3D424C", "#F2F3F5", "#B7BECA", "#E7E5DC", "#26272D", "#343943", "#FFBEA4");
        public static readonly UiTheme Green = new UiTheme(2, "#F7F6EB", "#EBEEDF", "#CFD6C0", "#304637", "#566A5B", "#3B5B46", "#FCFAEE", "#DFE6D5", "#934C37");
        public static readonly UiTheme Blue = new UiTheme(3, "#EDF7FC", "#DDEEF7", "#AACBDC", "#102F47", "#365A71", "#125879", "#F5FDFF", "#CFE6F3", "#A04232");
        public static readonly UiTheme Silver = new UiTheme(4, "#F2F3F8", "#E3E6EF", "#BFC5D6", "#252D42", "#48556F", "#404C69", "#FCFCFF", "#D8DDEA", "#953C4C");
        public static readonly UiTheme Red = new UiTheme(5, "#FFF2ED", "#F8E0DA", "#D8A7A1", "#462522", "#70423D", "#993D3C", "#FFF8F5", "#F3D2CB", "#96362E");
        public static readonly UiTheme Pink = new UiTheme(6, "#FFF2F7", "#F9DFEC", "#DEAFC7", "#482739", "#6F425A", "#8A3E68", "#FFF9FC", "#F4D0E2", "#9E3D50");
        static readonly UiTheme[] Options = { White, Black, Green, Blue, Silver, Red, Pink };
        public static readonly string[] Names = { "白色", "黑色", "青绿", "海蓝", "银色", "红色", "粉色" };

        UiTheme(int index, string background, string surface, string border, string text, string muted, string accent, string accentInk, string hover, string alert) {
            Index = index;
            Background = ColorTranslator.FromHtml(background);
            Surface = ColorTranslator.FromHtml(surface);
            Border = ColorTranslator.FromHtml(border);
            Text = ColorTranslator.FromHtml(text);
            Muted = ColorTranslator.FromHtml(muted);
            Accent = ColorTranslator.FromHtml(accent);
            AccentInk = ColorTranslator.FromHtml(accentInk);
            Hover = ColorTranslator.FromHtml(hover);
            Alert = ColorTranslator.FromHtml(alert);
        }

        public static UiTheme FromIndex(int index) { return index >= 0 && index < Options.Length ? Options[index] : White; }
    }

    public enum UiGlyph { None, Compact, Settings, Minimize, Close, Chevron, Plus }

    public sealed class RoundedDrawer : Panel {
        public RoundedDrawer() { DoubleBuffered=true; SetStyle(ControlStyles.ResizeRedraw,true); }
        protected override void OnPaintBackground(PaintEventArgs e) {
            using (Brush baseFill=new SolidBrush(Parent==null?BackColor:Parent.BackColor)) e.Graphics.FillRectangle(baseFill,e.ClipRectangle);
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using (GraphicsPath shape=SoftButton.Round(ClientRectangle,(int)Math.Round(12*e.Graphics.DpiX/96f)))
            using (Brush panelFill=new SolidBrush(BackColor)) e.Graphics.FillPath(panelFill,shape);
        }
    }

    public static class UiGlyphs {
        public static void Draw(Graphics graphics, UiGlyph glyph, Rectangle bounds, Color color) {
            GraphicsState state = graphics.Save();
            try {
                graphics.TranslateTransform(bounds.X, bounds.Y);
                graphics.ScaleTransform(bounds.Width / 24f, bounds.Height / 24f);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen pen = new Pen(color, 1.7f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round }) {
                    switch (glyph) {
                        case UiGlyph.Compact:
                            using (GraphicsPath path = SoftButton.Round(new Rectangle(2, 5, 20, 14), 3)) graphics.DrawPath(pen, path);
                            graphics.DrawLine(pen, 11, 13.5f, 17.5f, 13.5f);
                            graphics.DrawLine(pen, 11, 16, 17.5f, 16);
                            break;
                        case UiGlyph.Settings:
                            PointF[] teeth = new PointF[32];
                            for (int i = 0; i < teeth.Length; i++) {
                                int step = i % 4;
                                double angle = i * Math.PI / 16;
                                float radius = step == 1 || step == 2 ? 9.6f : 7.3f;
                                teeth[i] = new PointF(12 + radius * (float)Math.Cos(angle), 12 + radius * (float)Math.Sin(angle));
                            }
                            graphics.DrawPolygon(pen, teeth);
                            graphics.DrawEllipse(pen, 8.6f, 8.6f, 6.8f, 6.8f);
                            break;
                        case UiGlyph.Minimize:
                            graphics.DrawLine(pen, 6, 13, 18, 13);
                            break;
                        case UiGlyph.Plus:
                            graphics.DrawLine(pen, 6, 12, 18, 12);
                            graphics.DrawLine(pen, 12, 6, 12, 18);
                            break;
                        case UiGlyph.Chevron:
                            graphics.DrawLines(pen, new[] { new PointF(6, 9), new PointF(12, 15), new PointF(18, 9) });
                            break;
                        case UiGlyph.Close:
                            graphics.DrawLine(pen, 7, 7, 17, 17);
                            graphics.DrawLine(pen, 7, 17, 17, 7);
                            break;
                    }
                }
            } finally { graphics.Restore(state); }
        }
    }

    public sealed class NumberField : Panel {
        public UiTheme Theme = UiTheme.White;
        public int Minimum = 1, Maximum = 180;
        readonly TextBox editor = new TextBox { BorderStyle = BorderStyle.None, MaxLength = 3 };
        readonly SoftButton decrement = new SoftButton { Glyph = UiGlyph.Minimize, AccessibleName = "减少", TabStop = false };
        readonly SoftButton increment = new SoftButton { Glyph = UiGlyph.Plus, AccessibleName = "增加", TabStop = false };
        int lastValue = 1;
        public int Value {
            get { int v;return int.TryParse(editor.Text,out v)?Math.Max(Minimum,Math.Min(Maximum,v)):lastValue; }
            set { lastValue=Math.Max(Minimum,Math.Min(Maximum,value));editor.Text=lastValue.ToString(); }
        }
        public NumberField() {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            Controls.AddRange(new Control[] { editor, decrement, increment });
            decrement.Click += delegate { Value--; };
            increment.Click += delegate { Value++; };
            editor.Leave += delegate { Value = Value; };
            editor.KeyDown += delegate(object sender, KeyEventArgs e) {
                if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down) {
                    Value += e.KeyCode == Keys.Up ? 1 : -1;
                    e.SuppressKeyPress = true;
                }
            };
        }
        public void ApplyTheme(UiTheme theme) {
            Theme=theme;
            editor.BackColor=theme.Background;editor.ForeColor=theme.Text;
            decrement.Theme=increment.Theme=theme;
            decrement.PaintBackground=increment.PaintBackground=theme.Background;
            decrement.Invalidate();increment.Invalidate();Invalidate();
        }
        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            int button = Height * 13 / 24;
            int gap = Math.Max(1, Height / 16);
            int inset = Math.Max(3, Height / 5);
            increment.SetBounds(Width-button-inset,(Height-button)/2,button,button);
            decrement.SetBounds(increment.Left-button-gap,increment.Top,button,button);
            editor.SetBounds(inset,(Height-editor.PreferredHeight)/2,Math.Max(1,decrement.Left-inset-gap),editor.PreferredHeight);
        }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int radius = Math.Max(3, Height / 3);
            using (GraphicsPath shape = SoftButton.Round(new Rectangle(0, 0, Width - 1, Height - 1), radius)) {
                using (Brush fill = new SolidBrush(Theme.Background)) e.Graphics.FillPath(fill, shape);
                using (Pen outline = new Pen(Theme.Border)) e.Graphics.DrawPath(outline, shape);
            }
        }
    }

    public sealed class ChoiceButton : SoftButton {
        public readonly System.Collections.Generic.List<string> Items = new System.Collections.Generic.List<string>();
        int selectedIndex = -1;
        ToolStripDropDown popup;
        ToolStripControlHost popupHost;
        ListBox list;
        public int SelectedIndex {
            get { return selectedIndex; }
            set { selectedIndex = value >= 0 && value < Items.Count ? value : -1; Text = selectedIndex < 0 ? "" : Items[selectedIndex]; }
        }
        public ChoiceButton() { LeftAligned = true; Click += delegate { OpenChoices(); }; }

        void OpenChoices() {
            if (Items.Count == 0) return;
            if (popup == null) CreatePopup();
            if (popup.Visible) { popup.Close(); return; }
            popup.BackColor = list.BackColor = Theme.Background;
            list.ForeColor = Theme.Text;
            list.BeginUpdate();
            try {
                list.Items.Clear();
                list.Items.AddRange(Items.ToArray());
                list.SelectedIndex = SelectedIndex;
            } finally { list.EndUpdate(); }
            popupHost.Size = list.Size = new Size(Width, Math.Min(8,Items.Count) * list.ItemHeight + 2);
            popup.Show(this, new Point(0,Height));
            list.Focus();
        }
        void CreatePopup() {
            list = new ListBox { Font = Font, BorderStyle = BorderStyle.None, IntegralHeight = false, AccessibleName = AccessibleName };
            popupHost = new ToolStripControlHost(list) { AutoSize = false, Margin = Padding.Empty, Padding = Padding.Empty };
            popup = new ToolStripDropDown { Padding = Padding.Empty };
            popup.Items.Add(popupHost);
            list.MouseUp += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) Choose(list.IndexFromPoint(e.Location)); };
            list.KeyDown += delegate(object sender, KeyEventArgs e) {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress=true;Choose(list.SelectedIndex); }
                else if (e.KeyCode == Keys.Escape) popup.Close();
            };
        }
        void Choose(int index) {
            if (index < 0 || index >= Items.Count) return;
            SelectedIndex = index;
            popup.Close();
        }
        protected override void OnVisibleChanged(EventArgs e) {
            if (!Visible && popup != null && popup.Visible) popup.Close();
            base.OnVisibleChanged(e);
        }
        protected override void Dispose(bool disposing) {
            // Closing a menu can run the WinForms menu dispatcher synchronously.
            // Keep the menu until its owning control is disposed.
            if (disposing && popup != null) {
                ToolStripDropDown current = popup;
                popup = null;
                if (current.Visible) current.Close();
                current.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public sealed class ThemeCheckBox : CheckBox {
        public UiTheme Theme = UiTheme.White;
        public ThemeCheckBox() {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }
        protected override void OnCheckedChanged(EventArgs e) { base.OnCheckedChanged(e); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e) {
            using (Brush background = new SolidBrush(BackColor)) e.Graphics.FillRectangle(background, e.ClipRectangle);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = e.Graphics.DpiX / 96f;
            int size = Math.Max(13, (int)Math.Round(15 * scale));
            Rectangle box = new Rectangle(1, (Height - size) / 2, size, size);
            using (GraphicsPath path = SoftButton.Round(box, Math.Max(2, size / 4))) {
                using (Brush fill = new SolidBrush(Checked ? Theme.Accent : Theme.Background)) e.Graphics.FillPath(fill, path);
                using (Pen edge = new Pen(Checked ? Theme.Accent : Theme.Muted)) e.Graphics.DrawPath(edge, path);
            }
            if (Checked) using (Pen check = new Pen(Theme.AccentInk, Math.Max(1f, 1.4f * scale)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round }) {
                e.Graphics.DrawLines(check, new[] {
                    new PointF(box.X + size * .25f, box.Y + size * .51f),
                    new PointF(box.X + size * .43f, box.Y + size * .69f),
                    new PointF(box.X + size * .77f, box.Y + size * .33f)
                });
            }
            int start = size + (int)(7 * scale);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(start, 0, Width - start, Height), Theme.Text,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.PreserveGraphicsTranslateTransform);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle, Theme.Text, BackColor);
        }
    }
}
