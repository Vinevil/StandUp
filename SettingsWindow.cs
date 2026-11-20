using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace StandUp {
    // The preference controls keep a fixed layout inside their own scroll viewport.
    // Opening settings or using its wheel never changes the reminder window's size.
    public sealed class SettingsWindow : Form {
        readonly Panel viewport = new Panel { AutoScroll = true, TabStop = false };
        readonly Label heading = new Label { Text = "设置", TextAlign = ContentAlignment.MiddleLeft };
        readonly SoftButton close = new SoftButton { Glyph = UiGlyph.Close, AccessibleName = "关闭设置" };
        readonly RoundedDrawer content;
        readonly float scale;
        readonly Action<int> arrangeContent;
        UiTheme theme = UiTheme.White;

        public SettingsWindow(RoundedDrawer content, float scale, Font font, Action<int> arrangeContent) {
            this.content = content;
            this.scale = scale;
            this.arrangeContent = arrangeContent;
            Text = "站起来 · 设置";
            Font = font;
            heading.Font = new Font(font.FontFamily, 11, FontStyle.Bold);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            Controls.AddRange(new Control[] { heading, close, viewport });
            viewport.Controls.Add(content);
            close.Click += delegate { Hide(); };
            WheelScroll.Attach(viewport, content);
            close.TabStop = false;
        }

        public void ApplyTheme(UiTheme selected) {
            theme = selected;
            BackColor = viewport.BackColor = theme.Background;
            heading.ForeColor = theme.Text;
            close.Theme = theme;
            close.Invalidate();
            Invalidate();
        }

        public void Present(Form owner) {
            Rectangle area = Screen.FromControl(owner).WorkingArea;
            int logicalWidth = Math.Max(360, Math.Min(445, (int)(owner.Width / scale)));
            int width = Px(logicalWidth);
            int height = Math.Min(Px(353), Math.Max(Px(120), area.Height - Px(20)));
            Bounds = new Rectangle(Math.Max(area.Left, Math.Min(owner.Left + (owner.Width - width) / 2, area.Right - width)),
                Math.Max(area.Top, Math.Min(owner.Top + (owner.Height - height) / 2, area.Bottom - height)), width, height);
            heading.SetBounds(Px(18), Px(8), Px(170), Px(30));
            close.SetBounds(Width - Px(48), Px(8), Px(30), Px(30));
            viewport.SetBounds(Px(16), Px(45), Width - Px(32), Height - Px(52));
            viewport.AutoScrollPosition = Point.Empty;
            int contentHeight = Px(292);
            int scrollbarWidth = contentHeight > viewport.Height ? SystemInformation.VerticalScrollBarWidth : 0;
            int contentWidth = Math.Max(1, viewport.Width - scrollbarWidth - (scrollbarWidth == 0 ? 0 : Px(4)));
            // Keep fields usable on short screens by preserving width when a scrollbar is needed.
            content.SetBounds(0, 0, contentWidth, contentHeight);
            arrangeContent((int)(contentWidth / scale));
            using (GraphicsPath mask = SoftButton.Round(ClientRectangle, Px(14))) Region = new Region(mask);
            if (!Visible) Show(owner);
            Activate();
        }

        int Px(int value) { return (int)Math.Round(scale * value); }
        protected override bool ProcessCmdKey(ref Message message, Keys keys) {
            if (keys == Keys.Escape) { Hide(); return true; }
            return base.ProcessCmdKey(ref message, keys);
        }
        protected override void OnFormClosing(FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); }
            base.OnFormClosing(e);
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath ring = SoftButton.Round(new Rectangle(0, 0, Width - 1, Height - 1), Px(14)))
            using (Pen outline = new Pen(theme.Border)) e.Graphics.DrawPath(outline, ring);
        }

        static class WheelScroll {
            public static void Attach(Panel viewport, Control control) {
                control.MouseWheel += delegate(object sender, MouseEventArgs e) {
                    HandledMouseEventArgs handled = e as HandledMouseEventArgs;
                    if (handled != null) handled.Handled = true;
                    if (!viewport.VerticalScroll.Visible) return;
                    int current = -viewport.AutoScrollPosition.Y;
                    int max = Math.Max(0, viewport.DisplayRectangle.Height - viewport.ClientSize.Height);
                    int step = Math.Max(12, control.Font.Height * 3);
                    viewport.AutoScrollPosition = new Point(0, Math.Max(0, Math.Min(max, current - e.Delta * step / 120)));
                };
                foreach (Control child in control.Controls) Attach(viewport, child);
            }
        }
    }
}
