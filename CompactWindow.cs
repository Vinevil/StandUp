using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace StandUp {
    // The original settings used -1 for left and 1 for right.
    internal enum DockEdge { None = 0, Left = -1, Right = 1, Top = -2, Bottom = 2 }

    internal static class CompactDock {
        internal static readonly DockEdge[] Edges = { DockEdge.Left, DockEdge.Right, DockEdge.Top, DockEdge.Bottom };

        internal static bool IsAllowed(DockEdge edge, AnchorStyles taskbars) {
            switch (edge) {
                case DockEdge.Left: return (taskbars & AnchorStyles.Left) == 0;
                case DockEdge.Right: return (taskbars & AnchorStyles.Right) == 0;
                case DockEdge.Top: return (taskbars & AnchorStyles.Top) == 0;
                case DockEdge.Bottom: return (taskbars & AnchorStyles.Bottom) == 0;
                default: return false;
            }
        }

        internal static DockEdge NearestEdge(Rectangle window, Rectangle screen, AnchorStyles taskbars, int threshold) {
            DockEdge nearest = DockEdge.None;
            int best = threshold + 1;
            foreach (DockEdge edge in Edges) {
                if (!IsAllowed(edge, taskbars)) continue;
                int distance;
                switch (edge) {
                    case DockEdge.Left: distance = window.Left - screen.Left; break;
                    case DockEdge.Right: distance = screen.Right - window.Right; break;
                    case DockEdge.Top: distance = window.Top - screen.Top; break;
                    default: distance = screen.Bottom - window.Bottom; break;
                }
                if (distance < best) { best = distance; nearest = edge; }
            }
            return nearest;
        }

        internal static Rectangle ExpandedBounds(Point origin, Size size, Rectangle screen, Rectangle work, DockEdge edge) {
            int x = Math.Max(work.Left, Math.Min(origin.X, work.Right - size.Width));
            int y = Math.Max(work.Top, Math.Min(origin.Y, work.Bottom - size.Height));
            switch (edge) {
                case DockEdge.Left: x = screen.Left; break;
                case DockEdge.Right: x = screen.Right - size.Width; break;
                case DockEdge.Top: y = screen.Top; break;
                case DockEdge.Bottom: y = screen.Bottom - size.Height; break;
            }
            return new Rectangle(new Point(x, y), size);
        }

        internal static Rectangle StripBounds(Rectangle full, DockEdge edge, int thickness) {
            switch (edge) {
                case DockEdge.Left: return new Rectangle(full.Left, full.Top, thickness, full.Height);
                case DockEdge.Right: return new Rectangle(full.Right - thickness, full.Top, thickness, full.Height);
                case DockEdge.Top: return new Rectangle(full.Left, full.Top, full.Width, thickness);
                case DockEdge.Bottom: return new Rectangle(full.Left, full.Bottom - thickness, full.Width, thickness);
                default: return full;
            }
        }
    }

    public sealed class CompactWindow : Form {
        private const int ExpandedWidth = 252;
        private const int ExpandedHeight = 58;
        private const int StripThickness = 4;

        private readonly float scale;
        private readonly Font captionFont;
        private readonly Font digitsFont;
        private readonly Font messageFont;
        private readonly Timer tuckTimer = new Timer();
        private readonly ToolTip tips = new ToolTip();
        private readonly CompactButton action;
        private readonly CompactButton expand;
        private readonly CompactButton minimize;
        private readonly CompactButton edgeTab;
        private bool hasPlacement;
        private bool dragging;
        private bool ignoreTabUntilExit;
        private bool holdForKeyboard;
        private bool activateOnShow;
        private bool placementUpdatePending;
        private DockEdge dockEdge;
        private Point dragOrigin;
        private Point cursorOrigin;
        private Phase phase;
        private bool paused;
        private string timerText = "";
        private UiTheme theme = UiTheme.White;

        public event Action ExpandRequested;
        public event Action ActionRequested;
        public event Action MinimizeRequested;

        public Point RememberedLocation { get; private set; }
        public int DockSide { get { return (int)dockEdge; } }
        public bool IsTucked { get; private set; }

        public CompactWindow(float uiScale) {
            scale = Math.Max(1, uiScale);
            captionFont = new Font("Microsoft YaHei UI", 8f);
            digitsFont = new Font("Consolas", 17f, FontStyle.Bold);
            messageFont = new Font("Microsoft YaHei UI", 13f, FontStyle.Bold);

            Text = "站起来 · 精简模式";
            AccessibleName = Text;
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            BackColor = theme.Surface;
            Cursor = Cursors.SizeAll;

            action = NewButton(Glyph.Pause, true, delegate { Raise(ActionRequested); });
            expand = NewButton(Glyph.Expand, false, delegate { Raise(ExpandRequested); });
            minimize = NewButton(Glyph.Minimize, false, delegate { Raise(MinimizeRequested); });
            edgeTab = NewButton(Glyph.EdgeStrip, false, delegate { Reveal(true); });
            expand.AccessibleName = "展开完整窗口";
            minimize.AccessibleName = "隐藏精简窗口";
            edgeTab.AccessibleName = "展开贴边的计时器";
            tips.SetToolTip(expand, "展开完整窗口");
            tips.SetToolTip(minimize, "隐藏小窗，可从通知区域恢复");
            tips.SetToolTip(edgeTab, "展开计时器");
            tips.SetToolTip(this, "拖到屏幕边缘可收起，任务栏所在边除外");

            tuckTimer.Interval = 420;
            tuckTimer.Tick += delegate {
                tuckTimer.Stop();
                if (Visible && DockSide != 0 && !dragging && !holdForKeyboard && !Bounds.Contains(Cursor.Position)) Tuck();
            };
            MouseEnter += PointerEntered;
            MouseLeave += PointerLeft;
            edgeTab.MouseEnter += delegate { if (IsTucked && !ignoreTabUntilExit) Reveal(false); };
            edgeTab.MouseLeave += delegate { if (IsTucked) ignoreTabUntilExit = false; };
            MouseDown += BeginDrag;
            MouseMove += MoveDrag;
            MouseUp += EndDrag;
            MouseCaptureChanged += delegate { if (dragging && !Capture) FinishDrag(); };

            Size = new Size(Px(ExpandedWidth), Px(ExpandedHeight));
            LayoutButtons();
            UpdateTimer(Phase.Working, false, "00:00");
        }

        private static void Raise(Action handler) {
            if (handler != null) handler();
        }

        private int Px(double value) {
            return (int)Math.Round(value * scale);
        }

        private CompactButton NewButton(Glyph glyph, bool primary, Action click) {
            CompactButton button = new CompactButton(glyph, primary, scale);
            Controls.Add(button);
            button.Click += delegate { click(); };
            button.MouseEnter += PointerEntered;
            button.MouseLeave += PointerLeft;
            return button;
        }

        public void ApplyTheme(UiTheme newTheme) {
            theme = newTheme ?? UiTheme.White;
            BackColor = theme.Surface;
            action.ApplyTheme(theme);
            expand.ApplyTheme(theme);
            minimize.ApplyTheme(theme);
            edgeTab.ApplyTheme(theme);
            Invalidate();
        }

        public void RestorePlacement(Point location, int dockSide) {
            RememberedLocation = location;
            dockEdge = Enum.IsDefined(typeof(DockEdge), dockSide) ? (DockEdge)dockSide : DockEdge.None;
            hasPlacement = true;
        }

        public void ShowCompact(Point suggestedLocation, bool activate) {
            if (!hasPlacement) RestorePlacement(suggestedLocation, 0);
            activateOnShow = activate;
            // Windows applies its minimum tracking size when it creates a top-level
            // window. Create the handle at full size before assigning strip bounds.
            if (!IsHandleCreated) CreateHandle();
            if (DockSide != 0 && !activate) Tuck();
            else Reveal(activate);
            if (!Visible) Show();
            if (activate) {
                Activate();
                action.Focus();
            }
        }

        public void Reveal(bool activate) {
            tuckTimer.Stop();
            if (!hasPlacement) RestorePlacement(Location, 0);
            PlaceWindow(false);
            holdForKeyboard = activate && dockEdge != DockEdge.None;
            if (activate && Visible) {
                Activate();
                action.Focus();
            }
        }

        public void UpdateTimer(Phase currentPhase, bool isPaused, string digits) {
            if (currentPhase == phase && isPaused == paused && digits == timerText) return;
            bool stateChanged = phase != currentPhase || paused != isPaused || timerText.Length == 0;
            phase = currentPhase;
            paused = isPaused;
            timerText = digits;
            if (stateChanged) {
                string actionName;
                if (phase == Phase.Alert) actionName = "开始活动";
                else if (phase == Phase.RestDone) actionName = "开始下一轮";
                else actionName = paused ? "继续计时" : "暂停计时";
                action.Glyph = phase == Phase.Alert || phase == Phase.RestDone || paused ? Glyph.Play : Glyph.Pause;
                action.Alert = phase == Phase.Alert;
                edgeTab.Alert = phase == Phase.Alert;
                action.AccessibleName = actionName;
                edgeTab.AccessibleName = phase == Phase.Alert ? "到点了，展开计时器" : "展开贴边的计时器";
                tips.SetToolTip(action, actionName);
                action.Invalidate();
                edgeTab.Invalidate();
            }
            if (!IsTucked && Visible) Invalidate(new Rectangle(Px(52), Px(6), Px(88), Px(46)));
        }

        private void PlaceWindow(bool tucked) {
            Screen display = Screen.FromRectangle(new Rectangle(RememberedLocation, ExpandedSize));
            PlaceWindow(tucked, display, TaskbarEdges.OnScreen(display));
        }

        private Size ExpandedSize { get { return new Size(Px(ExpandedWidth), Px(ExpandedHeight)); } }

        private void PlaceWindow(bool tucked, Screen display, AnchorStyles taskbars) {
            if (!CompactDock.IsAllowed(dockEdge, taskbars)) dockEdge = DockEdge.None;
            Rectangle full = CompactDock.ExpandedBounds(RememberedLocation, ExpandedSize, display.Bounds, display.WorkingArea, dockEdge);
            RememberedLocation = full.Location;
            IsTucked = tucked && dockEdge != DockEdge.None;
            // Moving the tab under the drag cursor must not immediately open it again.
            ignoreTabUntilExit = IsTucked;
            SuspendLayout();
            try {
                Bounds = IsTucked ? CompactDock.StripBounds(full, dockEdge, Px(StripThickness)) : full;
                LayoutButtons();
            } finally { ResumeLayout(false); }
            ignoreTabUntilExit = IsTucked && Bounds.Contains(Cursor.Position);
        }

        private void LayoutButtons() {
            action.SetBounds(Px(139), Px(11), Px(36), Px(36));
            expand.SetBounds(Px(181), Px(14), Px(27), Px(30));
            minimize.SetBounds(Px(210), Px(14), Px(23), Px(30));
            edgeTab.SetBounds(0, 0, Width, Height);
            action.Visible = !IsTucked;
            expand.Visible = !IsTucked;
            minimize.Visible = !IsTucked;
            edgeTab.Visible = IsTucked;
            int radius = IsTucked ? (Math.Min(Width, Height) + 1) / 2 : Px(17);
            using (GraphicsPath shape = SoftButton.Round(ClientRectangle, radius)) {
                Region = new Region(shape);
            }
            Invalidate();
        }

        private void BeginDrag(object sender, MouseEventArgs e) {
            if (e.Button != MouseButtons.Left || IsTucked) return;
            if (e.Clicks == 2) {
                Raise(ExpandRequested);
                return;
            }
            tuckTimer.Stop();
            cursorOrigin = PointToScreen(e.Location);
            dragOrigin = Location;
            dragging = true;
            Capture = true;
        }

        private void MoveDrag(object sender, MouseEventArgs e) {
            if (!dragging) return;
            Point pointer = Cursor.Position;
            Location = new Point(dragOrigin.X + pointer.X - cursorOrigin.X, dragOrigin.Y + pointer.Y - cursorOrigin.Y);
        }

        private void EndDrag(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left && dragging) FinishDrag();
        }

        private void FinishDrag() {
            dragging = false;
            Capture = false;
            Point pointer = Cursor.Position;
            Screen display = Screen.FromPoint(pointer);
            AnchorStyles taskbars = TaskbarEdges.OnScreen(display);
            dockEdge = CompactDock.NearestEdge(Bounds, display.Bounds, taskbars, Px(22));
            RememberedLocation = Location;
            holdForKeyboard = false;
            PlaceWindow(dockEdge != DockEdge.None, display, taskbars);
        }

        private void Tuck() {
            tuckTimer.Stop();
            if (DockSide == 0) return;
            holdForKeyboard = false;
            PlaceWindow(true);
        }

        private void PointerEntered(object sender, EventArgs e) {
            if (!IsTucked) holdForKeyboard = false;
            tuckTimer.Stop();
        }

        private void PointerLeft(object sender, EventArgs e) {
            if (DockSide != 0 && !IsTucked && !dragging && !holdForKeyboard && !Bounds.Contains(Cursor.Position)) tuckTimer.Start();
        }

        protected override void OnVisibleChanged(EventArgs e) {
            if (!Visible) tuckTimer.Stop();
            base.OnVisibleChanged(e);
        }

        protected override bool ShowWithoutActivation { get { return !activateOnShow; } }

        protected override void WndProc(ref Message message) {
            base.WndProc(ref message);
            // Revalidate a live dock when the work area or monitor layout changes.
            if ((message.Msg == 0x001A || message.Msg == 0x007E) && Visible && hasPlacement && !placementUpdatePending) {
                placementUpdatePending = true;
                BeginInvoke((MethodInvoker)delegate {
                    placementUpdatePending = false;
                    if (!IsDisposed && Visible && !dragging) PlaceWindow(IsTucked);
                });
            }
        }

        protected override void OnDeactivate(EventArgs e) {
            holdForKeyboard = false;
            if (Visible) PointerLeft(this, EventArgs.Empty);
            base.OnDeactivate(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
            if (IsTucked && (keyData == Keys.Enter || keyData == Keys.Space)) {
                Reveal(true);
                return true;
            }
            if (keyData == Keys.Escape && DockSide != 0) {
                Tuck();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing && MinimizeRequested != null) {
                e.Cancel = true;
                Raise(MinimizeRequested);
            }
            base.OnFormClosing(e);
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if (IsTucked) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath border = SoftButton.Round(new Rectangle(0, 0, Width - 1, Height - 1), Px(17)))
            using (Pen pen = new Pen(theme.Border)) g.DrawPath(pen, border);
            Rectangle medallion = new Rectangle(Px(11), Px(11), Px(36), Px(36));
            if (e.ClipRectangle.IntersectsWith(medallion)) ThemeMedallion.Draw(g, medallion, theme.Index);

            bool message = phase == Phase.Alert || phase == Phase.RestDone;
            string caption;
            if (phase == Phase.Alert) caption = "活动提醒";
            else if (phase == Phase.RestDone) caption = "活动结束";
            else if (paused) caption = "已暂停";
            else caption = phase == Phase.Resting ? "活动倒计时" : "下次起身";
            string display = phase == Phase.Alert ? "站起来" : phase == Phase.RestDone ? "休息完成" : timerText;
            Color color = phase == Phase.Alert ? theme.Alert : theme.Text;
            const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
            TextRenderer.DrawText(g, caption, captionFont, new Rectangle(Px(55), Px(8), Px(82), Px(15)), theme.Muted, flags);
            TextRenderer.DrawText(g, display, message ? messageFont : digitsFont, new Rectangle(Px(54), Px(22), Px(84), Px(26)), color, flags);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                tuckTimer.Dispose();
                tips.Dispose();
                captionFont.Dispose();
                digitsFont.Dispose();
                messageFont.Dispose();
            }
            base.Dispose(disposing);
        }

        private enum Glyph { Pause, Play, Expand, Minimize, EdgeStrip }

        private sealed class CompactButton : Button {
            private readonly bool primary;
            private readonly float scale;
            private bool hover;
            private UiTheme theme = UiTheme.White;
            public Glyph Glyph;
            public bool Alert;

            public CompactButton(Glyph glyph, bool primary, float scale) {
                Glyph = glyph;
                this.primary = primary;
                this.scale = scale;
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                DoubleBuffered = true;
                Cursor = Cursors.Hand;
            }

            public void ApplyTheme(UiTheme newTheme) {
                theme = newTheme;
                BackColor = theme.Surface;
                Invalidate();
            }

            protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaint(PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color accent = Alert ? theme.Alert : theme.Accent;
                if (Glyph == Glyph.EdgeStrip) { g.Clear(accent); return; }
                g.Clear(theme.Surface);
                if (primary) {
                    Color fill = hover ? ControlPaint.Light(accent, 0.15f) : accent;
                    using (Brush brush = new SolidBrush(fill)) g.FillEllipse(brush, 1, 1, Width - 2, Height - 2);
                } else if (hover) {
                    using (GraphicsPath path = SoftButton.Round(new Rectangle(0, 0, Width, Height), (int)(8 * scale)))
                    using (Brush brush = new SolidBrush(theme.Hover)) g.FillPath(brush, path);
                }

                float x = Width / 2f;
                float y = Height / 2f;
                float a = 5f * scale;
                Color ink = primary ? theme.AccentInk : theme.Text;
                using (Pen p = new Pen(ink, 1.7f * scale)) {
                    p.StartCap = p.EndCap = LineCap.Round;
                    p.LineJoin = LineJoin.Round;
                    switch (Glyph) {
                        case Glyph.Pause:
                            g.DrawLine(p, x - a / 2, y - a, x - a / 2, y + a);
                            g.DrawLine(p, x + a / 2, y - a, x + a / 2, y + a);
                            break;
                        case Glyph.Play:
                            using (Brush brush = new SolidBrush(ink))
                                g.FillPolygon(brush, new[] { new PointF(x - a * .6f, y - a), new PointF(x + a, y), new PointF(x - a * .6f, y + a) });
                            break;
                        case Glyph.Expand:
                            g.DrawLines(p, new[] { new PointF(x + a, y + a * .1f), new PointF(x + a, y - a), new PointF(x - a * .1f, y - a) });
                            g.DrawLines(p, new[] { new PointF(x - a, y - a * .1f), new PointF(x - a, y + a), new PointF(x + a * .1f, y + a) });
                            break;
                        case Glyph.Minimize:
                            g.DrawLine(p, x - a, y, x + a, y);
                            break;
                    }
                }
                if (Focused && ShowFocusCues) {
                    int inset = 3;
                    using (Pen ring = new Pen(ink) { DashStyle = DashStyle.Dot })
                        g.DrawRectangle(ring, inset, inset, Width - 2 * inset - 1, Height - 2 * inset - 1);
                }
            }
        }
    }
}
