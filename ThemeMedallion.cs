using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace StandUp {
    // Each medallion is an original illustration on the same 64-unit canvas.
    public static class ThemeMedallion {
        private static readonly RectangleF Disc = new RectangleF(2, 2, 60, 60);
        private static readonly string[] UpperSky = { "#C8E6F2", "#121831", "#CEE9C0", "#C1E9FC", "#1E2539", "#F7D39A", "#F8CFDE" };
        private static readonly string[] LowerSky = { "#FEF1D1", "#44567E", "#EAF2CD", "#E2F6ED", "#666F89", "#FCEDD4", "#FFF1EA" };
        private static readonly string[] Rims = { "#89ADBA", "#8F9BC4", "#789865", "#64A9C7", "#BAC3D4", "#C88954", "#C689A6" };

        public static void Draw(Graphics graphics, Rectangle bounds, int themeIndex) {
            int size = Math.Min(bounds.Width, bounds.Height);
            if (size <= 0) return;
            if (themeIndex < 0 || themeIndex >= UpperSky.Length) themeIndex = 0;

            GraphicsState original = graphics.Save();
            try {
                graphics.TranslateTransform(bounds.X + (bounds.Width - size) / 2f, bounds.Y + (bounds.Height - size) / 2f);
                graphics.ScaleTransform(size / 64f, size / 64f);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (GraphicsPath mask = new GraphicsPath()) {
                    mask.AddEllipse(Disc);
                    GraphicsState clipped = graphics.Save();
                    try {
                        graphics.SetClip(mask, CombineMode.Intersect);
                        using (Brush sky = new LinearGradientBrush(Disc, ColorOf(UpperSky[themeIndex]), ColorOf(LowerSky[themeIndex]), 90f))
                            graphics.FillEllipse(sky, Disc);

                        switch (themeIndex) {
                            case 1: DrawMoon(graphics); break;
                            case 2: DrawTree(graphics); break;
                            case 3: DrawOcean(graphics); break;
                            case 4: DrawGalaxy(graphics); break;
                            case 5: DrawFlowerField(graphics, false); break;
                            case 6: DrawFlowerField(graphics, true); break;
                            default: DrawDay(graphics); break;
                        }
                    } finally { graphics.Restore(clipped); }

                    using (Pen rim = new Pen(ColorOf(Rims[themeIndex]), size <= 40 ? 2f : 1.5f))
                        graphics.DrawEllipse(rim, new RectangleF(2.5f, 2.5f, 59, 59));
                }
            } finally { graphics.Restore(original); }
        }

        private static void DrawDay(Graphics g) {
            DiscOf(g, "#FFF3C9", 32, 9, 24, 24);
            DiscOf(g, "#FFCB64", 36, 13, 16, 16);
            Stroke(g, "#FFFDF3", 2, P(10, 25), P(23, 25));
            Stroke(g, "#FFFDF3", 2, P(16, 21), P(28, 21));
            Polygon(g, "#91A8BD", P(-2, 46), P(17, 27), P(28, 37), P(36, 33), P(56, 47), P(66, 44), P(66, 64), P(-2, 64));
            Polygon(g, "#ECF1EA", P(10, 34), P(17, 27), P(25, 34), P(18, 32), P(15, 35), P(15, 32));
            Hill(g, "#82B4A7", P(-3, 49), P(15, 46), P(26, 34), P(42, 44), P(54, 49), P(59, 42), P(67, 42));
            Hill(g, "#42867E", P(-3, 48), P(14, 41), P(32, 56), P(47, 51), P(57, 50), P(64, 49), P(69, 49));
            Stroke(g, "#E4E9C7", 1.7f, P(20, 54), P(30, 57), P(38, 57));
        }

        private static void DrawMoon(Graphics g) {
            Star(g, "#DDE5FD", 13, 21, 2.8f);
            Star(g, "#EFF0CE", 49, 23, 2.7f);
            DiscOf(g, "#BECDE9", 19, 12, 2.1f, 2.1f);
            DiscOf(g, "#B9C7E6", 54, 32, 2.1f, 2.1f);
            DiscOf(g, "#B6C6E4", 18, 35, 1.8f, 1.8f);

            using (GraphicsPath moon = new GraphicsPath()) {
                moon.AddBezier(39, 10, 24, 10, 18, 23, 24, 34);
                moon.AddBezier(24, 34, 30, 45, 45, 46, 52, 34);
                moon.AddBezier(52, 34, 40, 38, 29, 24, 39, 10);
                moon.CloseFigure();
                using (Brush light = new LinearGradientBrush(new PointF(24, 10), new PointF(47, 47), ColorOf("#FFF4C9"), ColorOf("#EBC880")))
                    g.FillPath(light, moon);
            }

            Polygon(g, "#455579", P(-2, 53), P(16, 40), P(25, 47), P(42, 43), P(57, 49), P(66, 43), P(66, 66), P(-2, 66));
            Hill(g, "#242F54", P(-2, 52), P(19, 48), P(28, 59), P(42, 53), P(51, 48), P(66, 46), P(69, 49));
            Stroke(g, "#7785A7", 1.4f, P(23, 55), P(28, 56), P(33, 56));
        }

        private static void DrawTree(Graphics g) {
            DiscOf(g, "#E9EDBC", 37, 8, 16, 16);
            Hill(g, "#A3C18E", P(-2, 52), P(14, 37), P(32, 47), P(39, 46), P(50, 45), P(57, 38), P(69, 37));
            Hill(g, "#719B6C", P(-2, 54), P(18, 46), P(26, 59), P(44, 53), P(52, 50), P(62, 49), P(69, 51));

            Polygon(g, "#67533B", P(26, 55), P(29, 45), P(30, 29), P(34, 29), P(35, 45), P(38, 54), P(43, 56));
            Stroke(g, "#67533B", 3.5f, P(32, 38), P(22, 29), P(21, 23));
            Stroke(g, "#67533B", 3.3f, P(33, 40), P(43, 27));
            DiscOf(g, "#426C51", 13, 22, 21, 17);
            DiscOf(g, "#426C51", 29, 20, 23, 19);
            DiscOf(g, "#527F54", 16, 15, 24, 18);
            DiscOf(g, "#5D915B", 32, 15, 18, 18);
            DiscOf(g, "#75A467", 23, 10, 20, 17);
            DiscOf(g, "#A5C284", 25, 13, 10, 5);
            Stroke(g, "#ABC78C", 1.7f, P(17, 53), P(21, 52));
            Stroke(g, "#ABC78C", 1.7f, P(43, 50), P(47, 49));
        }

        private static void DrawOcean(Graphics g) {
            DiscOf(g, "#FFF3C4", 12, 12, 13, 13);
            Stroke(g, "#8EC8DE", 1.4f, P(39, 18), P(42, 16), P(45, 17), P(48, 15));
            Stroke(g, "#FBFFFF", 1.5f, P(9, 31), P(25, 31));

            Hill(g, "#58A8C7", P(-2, 35), P(9, 33), P(22, 36), P(33, 33), P(45, 31), P(52, 37), P(66, 34));
            using (GraphicsPath wave = new GraphicsPath()) {
                wave.AddBezier(-3, 47, 14, 56, 18, 35, 30, 28);
                wave.AddBezier(30, 28, 40, 18, 53, 25, 55, 33);
                wave.AddBezier(55, 33, 42, 29, 37, 34, 42, 41);
                wave.AddBezier(42, 41, 47, 46, 57, 49, 68, 46);
                wave.AddLine(68, 46, 68, 65);
                wave.AddLine(68, 65, -3, 65);
                wave.CloseFigure();
                using (Brush foamBlue = new LinearGradientBrush(new PointF(0, 25), new PointF(0, 65), ColorOf("#41AAC8"), ColorOf("#235887")))
                    g.FillPath(foamBlue, wave);
            }
            using (GraphicsPath foam = new GraphicsPath()) {
                foam.AddBezier(14, 43, 23, 40, 21, 29, 32, 25);
                foam.AddBezier(32, 25, 42, 19, 51, 25, 54, 32);
                foam.AddBezier(54, 32, 45, 27, 41, 28, 37, 31);
                foam.AddBezier(37, 31, 34, 32, 34, 27, 31, 32);
                foam.AddBezier(31, 32, 27, 38, 22, 42, 14, 43);
                foam.CloseFigure();
                Fill(g, "#F0FFFE", foam);
            }
            Stroke(g, "#96E2E4", 1.9f, P(17, 53), P(28, 52), P(36, 55), P(46, 53));
            DiscOf(g, "#E0F6FA", 57, 36, 2.3f, 2.3f);
            DiscOf(g, "#E0F6FA", 49, 36, 1.5f, 1.5f);
        }

        private static void DrawGalaxy(Graphics g) {
            Star(g, "#EFF5FC", 17, 17, 3.8f);
            Star(g, "#C2D3EF", 50, 45, 3.2f);
            DiscOf(g, "#D0DBEF", 49, 15, 1.9f, 1.9f);
            DiscOf(g, "#A2AFCC", 54, 23, 1.9f, 1.9f);
            DiscOf(g, "#C0CCE0", 21, 49, 1.8f, 1.8f);
            DiscOf(g, "#D5DFF0", 37, 12, 1.6f, 1.6f);
            DiscOf(g, "#BCC8E5", 12, 42, 1.9f, 1.9f);
            DiscOf(g, "#A5B5CF", 39, 50, 1.5f, 1.5f);

            GraphicsState rotated = g.Save();
            try {
                g.TranslateTransform(32.5f, 32);
                g.RotateTransform(-29);
                using (GraphicsPath glow = new GraphicsPath()) {
                    glow.AddEllipse(-25f, -11, 50, 22);
                    using (PathGradientBrush light = new PathGradientBrush(glow)) {
                        light.CenterColor = Color.FromArgb(212, 220, 224, 244);
                        light.SurroundColors = new[] { Color.FromArgb(0, 187, 199, 228) };
                        g.FillPath(light, glow);
                    }
                }
                using (Pen ring = new Pen(Color.FromArgb(205, 220, 229), 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round }) {
                    g.DrawArc(ring, -24, -10, 50, 20, 182, 129);
                    g.DrawArc(ring, -22, -9, 44, 18, 26, 128);
                }
                using (Pen ring = new Pen(Color.FromArgb(188, 199, 229), 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round }) {
                    g.DrawArc(ring, -19, -6, 38, 14, 203, 117);
                    g.DrawArc(ring, -17, -5, 34, 12, 29, 145);
                }
                DiscOf(g, "#F9F4EB", -5, -2.7f, 10, 5.4f);
            } finally { g.Restore(rotated); }
        }

        private static void DrawFlowerField(Graphics g, bool pink) {
            DiscOf(g, pink ? "#FFF4EC" : "#FFE4A9", 34, 9, 17, 17);
            Hill(g, pink ? "#D8B1BF" : "#D3A877", P(-2, 38), P(12, 25), P(24, 40), P(32, 37), P(44, 32), P(53, 25), P(68, 34));
            Hill(g, pink ? "#8DA181" : "#ADB77D", P(-2, 44), P(12, 37), P(30, 48), P(39, 42), P(51, 35), P(65, 36), P(68, 40));
            Hill(g, pink ? "#647B6A" : "#657550", P(-2, 55), P(8, 45), P(19, 55), P(31, 53), P(47, 47), P(58, 43), P(67, 46));

            if (pink) {
                DiscOf(g, "#F3B7CD", 13, 44, 4, 3.5f);
                DiscOf(g, "#E996B3", 46, 51, 4, 3.5f);
                DiscOf(g, "#F1B3CC", 39, 44, 3, 2.8f);
                Flower(g, true, 18, 36, 0.65f);
                Flower(g, true, 30, 45, 0.95f);
                Flower(g, true, 48, 34, 0.8f);
            } else {
                DiscOf(g, "#D26453", 17, 43, 4, 3);
                DiscOf(g, "#E67552", 37, 50, 3.5f, 3.3f);
                DiscOf(g, "#CB543F", 45, 41, 4, 3.5f);
                Flower(g, false, 12, 47, 0.65f);
                Flower(g, false, 28, 37, 1f);
                Flower(g, false, 51, 48, 0.83f);
            }
        }

        private static void Flower(Graphics g, bool pink, float x, float y, float scale) {
            GraphicsState position = g.Save();
            try {
                g.TranslateTransform(x, y);
                g.ScaleTransform(scale, scale);
                Stroke(g, "#475F48", 2.3f, P(0, 2), P(-1, 10), P(-2, 19));
                DiscOf(g, "#748F63", -8, 10, 7, 3.8f);
                DiscOf(g, "#92A874", -2, 7, 7.6f, 3.3f);
                if (pink) {
                    for (int i = 0; i < 5; i++) {
                        double angle = Math.PI * (i * 0.4 - 0.5);
                        float cx = (float)Math.Cos(angle) * 4.8f;
                        float cy = (float)Math.Sin(angle) * 4.8f;
                        DiscOf(g, i < 3 ? "#F4A8C7" : "#E085AE", cx - 3.6f, cy - 3.6f, 7.2f, 7.2f);
                    }
                    DiscOf(g, "#FFF0B8", -2.4f, -2.4f, 4.8f, 4.8f);
                } else {
                    DiscOf(g, "#BA3E39", -7.7f, -3.8f, 15.4f, 9.6f);
                    DiscOf(g, "#F27958", -7, -6.4f, 8.2f, 9.9f);
                    DiscOf(g, "#E75E49", -1.3f, -6.8f, 8.1f, 10.6f);
                    DiscOf(g, "#854036", -2.2f, -1.1f, 4.4f, 3.6f);
                    DiscOf(g, "#FBC479", -1.1f, -0.9f, 2.2f, 1.9f);
                }
            } finally { g.Restore(position); }
        }

        private static Color ColorOf(string hex) { return ColorTranslator.FromHtml(hex); }
        private static PointF P(float x, float y) { return new PointF(x, y); }

        private static void DiscOf(Graphics g, string color, float x, float y, float width, float height) {
            using (Brush fill = new SolidBrush(ColorOf(color))) g.FillEllipse(fill, x, y, width, height);
        }

        private static void Polygon(Graphics g, string color, params PointF[] vertices) {
            using (Brush fill = new SolidBrush(ColorOf(color))) g.FillPolygon(fill, vertices);
        }

        private static void Fill(Graphics g, string color, GraphicsPath shape) {
            using (Brush fill = new SolidBrush(ColorOf(color))) g.FillPath(fill, shape);
        }

        private static void Stroke(Graphics g, string color, float width, params PointF[] points) {
            using (Pen pen = new Pen(ColorOf(color), width) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                g.DrawLines(pen, points);
        }

        private static void Star(Graphics g, string color, float x, float y, float size) {
            Polygon(g, color, P(x, y - size), P(x + 0.9f, y - 0.9f), P(x + size, y), P(x + 0.9f, y + 0.9f),
                P(x, y + size), P(x - 0.9f, y + 0.9f), P(x - size, y), P(x - 0.9f, y - 0.9f));
        }

        private static void Hill(Graphics g, string color, params PointF[] profile) {
            using (GraphicsPath shape = new GraphicsPath()) {
                shape.AddBeziers(profile);
                PointF start = profile[0];
                PointF end = profile[profile.Length - 1];
                shape.AddLine(end, P(end.X, 66));
                shape.AddLine(P(end.X, 66), P(start.X, 66));
                shape.CloseFigure();
                Fill(g, color, shape);
            }
        }
    }
}
