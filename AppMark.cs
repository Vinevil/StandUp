using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace StandUp {
    public static class AppMark {
        private const string IconResource = "standup.icon.ico";
        private static readonly int[] FrameSizes = { 16, 20, 24, 32, 40, 48, 64, 96, 128, 256 };
        // Points on the two rising curves where the skyline meets the arrow.
        private static readonly PointF LeftFoot = new PointF(294, 654);
        private static readonly PointF RightFoot = new PointF(705, 357);

        private static Stream OpenEmbeddedIcon() {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(IconResource);
            if (stream == null) throw new InvalidOperationException("程序缺少内嵌图标：" + IconResource);
            return stream;
        }

        public static Icon CreateIcon() {
            return CreateIcon(System.Windows.Forms.SystemInformation.IconSize);
        }

        public static Icon CreateIcon(Size displaySize) {
            using (Stream stream = OpenEmbeddedIcon())
            using (Icon source = new Icon(stream, displaySize)) return (Icon)source.Clone();
        }

        public static void SaveIcon(string path) {
            using (Stream source = OpenEmbeddedIcon())
            using (Stream destination = File.Create(path)) source.CopyTo(destination);
        }

        public static void SaveClockIcon(string iconPath) {
            byte[][] frames = new byte[FrameSizes.Length][];
            for (int i = 0; i < FrameSizes.Length; i++) {
                using (Bitmap image = CreateClockFrame(FrameSizes[i])) {
                    if (FrameSizes[i] < 256) frames[i] = EncodeBitmapFrame(image);
                    else using (MemoryStream stream = new MemoryStream()) {
                        image.Save(stream, ImageFormat.Png);
                        frames[i] = stream.ToArray();
                    }
                }
            }
            WriteIcon(iconPath, frames);
        }

        static Pen Stroke(Color color, float width) {
            return new Pen(color, width) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        }

        static Bitmap CreateClockFrame(int size) {
            // Only used when exporting the ICO. Render each weight at four times its
            // target resolution for even coverage on diagonals and pointed strokes.
            using (Bitmap artwork = new Bitmap(size * 4, size * 4, PixelFormat.Format32bppArgb)) {
                using (Graphics graphics = Graphics.FromImage(artwork)) {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    float canvas = 700f + Math.Max(0, 48 - size) * 1.4f + Math.Max(0, 32 - size) * 3.5f;
                    float scale = artwork.Width / canvas;
                    float centerOffset = (canvas / 2 - 512) * scale;
                    using (Matrix placement = new Matrix(scale, 0, 0, scale, centerOffset, centerOffset))
                        graphics.Transform = placement;

                    // A proportional stroke plus a pixel allowance keeps small shell icons dark.
                    float width = 26f + 590f / size;
                    using (Pen ink = Stroke(Color.Black, width)) {
                        DrawClockMark(graphics, ink, size);
                    }
                }
                Bitmap result = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                try {
                    using (Graphics output = Graphics.FromImage(result)) {
                        output.CompositingMode = CompositingMode.SourceCopy;
                        output.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        output.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        output.DrawImage(artwork, new Rectangle(0, 0, size, size),
                            0, 0, artwork.Width, artwork.Height, GraphicsUnit.Pixel);
                    }
                    return result;
                } catch { result.Dispose(); throw; }
            }
        }

        static PointF DialPoint(float radius, double degrees) {
            double angle = degrees * Math.PI / 180;
            return new PointF(512 + (float)(radius * Math.Cos(angle)),
                              512 + (float)(radius * Math.Sin(angle)));
        }

        static void DrawClockMark(Graphics graphics, Pen ink, int size) {
            const int outerRadius = 309;
            RectangleF rim = Ring(512, outerRadius);
            bool tiny = size <= 24;
            float rimStart = tiny ? 162 : 154;
            graphics.DrawArc(ink, rim, rimStart, (tiny ? 287 : 297) - rimStart);

            float peakInset = Math.Max(6f, 1200f / size - 6f);
            PointF leftPeak = DialPoint(275 - peakInset, 181), leftValley = DialPoint(135, 205);
            PointF centerPeak = DialPoint(276 - peakInset, 225), rightValley = DialPoint(128, 259);
            PointF rightPeak = DialPoint(272 - peakInset, 274);
            float hourWidth = tiny ? 65 : 54, minuteWidth = tiny ? 48 : 38;
            // Keep the transparent interiors of the hands clear where the ridge passes behind them.
            using (Pen detail = Stroke(Color.Black, ink.Width * .76f))
            using (GraphicsPath hands = new GraphicsPath()) {
                AddHand(hands, 205, 200, hourWidth);
                AddHand(hands, 259, 265, minuteWidth);
                GraphicsState skylineState = graphics.Save();
                graphics.SetClip(hands, CombineMode.Exclude);
                float clearance = Math.Max(14f, 620f / size);
                using (GraphicsPath margin = (GraphicsPath)hands.Clone())
                using (Pen outline = Stroke(Color.Black, clearance * 2)) {
                    margin.Widen(outline);
                    graphics.SetClip(margin, CombineMode.Exclude);
                }
                graphics.DrawLines(detail, new[] { LeftFoot, leftPeak, leftValley, centerPeak,
                    rightValley, rightPeak, RightFoot });
                graphics.Restore(skylineState);
                if (size <= 64) graphics.FillPath(Brushes.Black, hands);
                else using (Pen handInk = Stroke(Color.Black, ink.Width * .59f))
                    graphics.DrawPath(handInk, hands);
                graphics.DrawEllipse(detail, Ring(512, 22));
            }

            // Both broken strokes have the same physical gap on the 4:30 radius.
            float innerRadius = outerRadius - Math.Max(60f, ink.Width + 870f / size);
            float gap = Math.Max(65f, 1450f / size) + ink.Width;
            DrawBrokenArc(graphics, ink, outerRadius, tiny ? -9 : -21, tiny ? 113 : 122, gap);
            DrawBrokenArc(graphics, ink, innerRadius, tiny ? 3 : -10, tiny ? 105 : 118, gap);

            DrawRisingArrow(graphics, ink, size);
        }

        static void AddHand(GraphicsPath hands, float degrees, float length, float width) {
            // A narrow neck, broad shoulder and pointed tip give the two blades a watch-hand profile.
            using (GraphicsPath blade = new GraphicsPath()) {
                float neck = width * 0.13f, shoulder = width / 2;
                blade.AddPolygon(new[] {
                    new PointF(21, -neck), new PointF(41, -neck), new PointF(length * .34f, -shoulder),
                    new PointF(length, 0), new PointF(length * .34f, shoulder),
                    new PointF(41, neck), new PointF(21, neck)
                });
                using (Matrix position = new Matrix()) {
                    position.Translate(512, 512);
                    position.Rotate(degrees);
                    blade.Transform(position);
                }
                hands.AddPath(blade, false);
            }
        }

        static void DrawBrokenArc(Graphics graphics, Pen ink, float radius, float start, float end, float gap) {
            float halfAngle = (float)(Math.Asin(gap / (2 * radius)) * 180 / Math.PI);
            RectangleF circle = Ring(512, radius);
            graphics.DrawArc(ink, circle, start, 45 - halfAngle - start);
            graphics.DrawArc(ink, circle, 45 + halfAngle, end - 45 - halfAngle);
        }

        static RectangleF Ring(float center, float radius) {
            return new RectangleF(center - radius, center - radius, radius * 2, radius * 2);
        }

        static void DrawRisingArrow(Graphics graphics, Pen ink, int size) {
            // Two independent sweeps: a short turning tail and an open, hooked arrow.
            using (GraphicsPath arrow = new GraphicsPath()) {
                arrow.AddBezier(new PointF(206, 814), new PointF(224, 758), new PointF(256, 700), LeftFoot);
                arrow.AddBezier(LeftFoot, new PointF(313, 632), new PointF(333, 613), new PointF(354, 599));
                arrow.AddBezier(354, 599, 390, 574, 420, 577, 446, 602);
                arrow.StartFigure();
                arrow.AddBezier(281, 814, 296, 766, 329, 717, 368, 690);
                arrow.AddBezier(368, 690, 411, 660, 478, 650, 536, 599);
                arrow.AddBezier(new PointF(536, 599), new PointF(592, 550), new PointF(651, 449), RightFoot);
                arrow.AddBezier(RightFoot, new PointF(719, 332), new PointF(733, 308), new PointF(746, 286));
                arrow.AddLines(new[] { new PointF(746, 286), new PointF(708, 261),
                    new PointF(816, 216), new PointF(817, 349), new PointF(789, 325) });
                float spread = Math.Max(0f, 1000f / size - 14f);
                arrow.AddBezier(789, 325, 748 + spread, 405, 685 + spread, 508, 645 + spread, 566);
                arrow.AddBezier(645 + spread, 566, 600 + spread, 628, 563, 654 + spread, 530, 649 + spread);
                using (Pen contour = (Pen)ink.Clone()) {
                    contour.LineJoin = LineJoin.Miter;
                    contour.MiterLimit = size <= 32 ? 1.7f : 3f;
                    graphics.DrawPath(contour, arrow);
                }
            }
        }

        // DIB frames keep alpha working with older shell and WinForms icon readers.
        private static byte[] EncodeBitmapFrame(Bitmap image) {
            int size = image.Width;
            int maskStride = ((size + 31) / 32) * 4;
            byte[] mask = new byte[maskStride * size];
            byte[] row = new byte[size * 4];
            BitmapData pixels = image.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try {
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter frame = new BinaryWriter(stream)) {
                    frame.Write(40);
                    frame.Write(size);
                    frame.Write(size * 2);
                    frame.Write((ushort)1);
                    frame.Write((ushort)32);
                    for (int i = 0; i < 6; i++) frame.Write(0);
                    for (int y = 0; y < size; y++) {
                        Marshal.Copy(IntPtr.Add(pixels.Scan0, (size - 1 - y) * pixels.Stride), row, 0, row.Length);
                        frame.Write(row);
                        for (int x = 0; x < size; x++)
                            if (row[x * 4 + 3] == 0) mask[y * maskStride + x / 8] |= (byte)(128 >> (x % 8));
                    }
                    frame.Write(mask);
                    return stream.ToArray();
                }
            } finally { image.UnlockBits(pixels); }
        }

        private static void WriteIcon(string path, byte[][] frames) {
            using (BinaryWriter icon = new BinaryWriter(File.Create(path))) {
                icon.Write((ushort)0);
                icon.Write((ushort)1);
                icon.Write((ushort)FrameSizes.Length);
                int imageOffset = 6 + 16 * FrameSizes.Length;
                for (int i = 0; i < FrameSizes.Length; i++) {
                    byte dimension = FrameSizes[i] == 256 ? (byte)0 : (byte)FrameSizes[i];
                    icon.Write(dimension);
                    icon.Write(dimension);
                    icon.Write((byte)0);
                    icon.Write((byte)0);
                    icon.Write((ushort)1);
                    icon.Write((ushort)32);
                    icon.Write(frames[i].Length);
                    icon.Write(imageOffset);
                    imageOffset += frames[i].Length;
                }
                foreach (byte[] frame in frames) icon.Write(frame);
            }
        }
    }
}
