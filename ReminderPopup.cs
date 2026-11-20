using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace StandUp {
    public sealed class ReminderPopup : Form {
        public event Action StartAction, SnoozeAction, ViewAction;
        readonly Label quote = new Label();
        readonly Label source = new Label();
        readonly Label heading = new Label();
        readonly float scale;
        UiTheme theme = UiTheme.White;

        public ReminderPopup(float scale) {
            this.scale = scale;
            Text = "站起来！";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(Px(342), Px(198));
            using (GraphicsPath shape = SoftButton.Round(ClientRectangle, Px(14))) Region = new Region(shape);

            heading.Text = "站起来！";
            heading.Font = new Font("Microsoft YaHei UI", 13, FontStyle.Bold);
            heading.TextAlign = ContentAlignment.MiddleLeft;
            Place(heading, 16, 12, 258, 29);
            quote.Font = new Font("Microsoft YaHei UI", 10);
            quote.AutoEllipsis = true;
            Place(quote, 17, 51, 308, 66);
            source.Font = new Font("Microsoft YaHei UI", 8);
            source.AutoEllipsis = true;
            Place(source, 17, 119, 308, 22);

            AddButton("", "关闭提醒气泡", 298, 11, 28, 28, false, delegate { Hide(); }).Glyph = UiGlyph.Close;
            AddButton("开始活动", "开始活动", 16, 153, 110, 33, true, delegate { if (StartAction != null) StartAction(); });
            AddButton("稍后 1 分钟", "稍后一分钟提醒", 132, 153, 103, 33, false, delegate { if (SnoozeAction != null) SnoozeAction(); });
            AddButton("打开窗口", "打开主窗口", 241, 153, 85, 33, false, delegate { if (ViewAction != null) ViewAction(); });
            ApplyTheme(theme);
        }

        int Px(int value) { return (int)Math.Round(value * scale); }
        void Place(Control control, int x, int y, int width, int height) {
            control.SetBounds(Px(x), Px(y), Px(width), Px(height));
            Controls.Add(control);
        }
        SoftButton AddButton(string text, string accessibleName, int x, int y, int width, int height, bool primary, Action click) {
            SoftButton button = new SoftButton { Text = text, AccessibleName = accessibleName, Primary = primary };
            button.Click += delegate { click(); };
            Place(button, x, y, width, height);
            return button;
        }
        public void ApplyTheme(UiTheme selected) {
            theme = selected;
            BackColor = theme.Background;
            heading.ForeColor = theme.Alert;
            quote.ForeColor = theme.Text;
            source.ForeColor = theme.Muted;
            foreach (Control control in Controls) {
                SoftButton button = control as SoftButton;
                if (button != null) { button.Theme = theme; button.Invalidate(); }
            }
            Invalidate();
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams {
            get { CreateParams parameters = base.CreateParams; parameters.ExStyle |= 0x80; return parameters; }
        }
        public void Present(Saying saying, Rectangle workingArea) {
            quote.Text = saying.Text;
            source.Text = PixelScene.JoinSource(saying.Person, saying.Source);
            Location = new Point(Math.Max(workingArea.Left, workingArea.Right - Width - Px(18)), Math.Max(workingArea.Top, workingArea.Bottom - Height - Px(18)));
            if (!Visible) Show();
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = SoftButton.Round(new Rectangle(0, 0, Width - 1, Height - 1), Px(14)))
                using (Pen pen = new Pen(theme.Border)) e.Graphics.DrawPath(pen, path);
        }
    }
}
