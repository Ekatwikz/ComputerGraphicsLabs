using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WPFDrawing {
    internal static class MyExtensions {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        public static BitmapSource ToBitmapSource(this Bitmap bitmap) { // TODO: remove me
            IntPtr hBitmap = IntPtr.Zero;
            try {
                hBitmap = bitmap.GetHbitmap();
                BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                  hBitmap,
                  IntPtr.Zero,
                  Int32Rect.Empty,
                  BitmapSizeOptions.FromEmptyOptions());
                return bitmapSource;
            } finally {
                DeleteObject(hBitmap);
            }
        }

        public static byte[] SerializeToArray(this WriteableBitmap bitmap) {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (MemoryStream stream = new MemoryStream()) {
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        public static WriteableBitmap DeserializeToWriteableBitmap(this byte[] imageData) {
            using (MemoryStream stream = new MemoryStream(imageData)) {
                BitmapDecoder decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                BitmapFrame frame = decoder.Frames[0];

                WriteableBitmap bitmap = new WriteableBitmap(frame);
                return bitmap;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color Invert(this Color color) {
            return Color.FromArgb(color.ToArgb() ^ 0xFFFFFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RGBCoord Invert(this RGBCoord rgbCoord) {
            return new RGBCoord {
                Coord = rgbCoord.Coord,
                CoordColor = rgbCoord.CoordColor.Invert(),
            };
        }

        public static MessageBoxResult DisplayAsMessageBox(this Exception ex, string titleInfo = "") {
            return MessageBox.Show(ex.Message,
                string.Format("{0}{1}",
                    ex.GetType(),
                    string.IsNullOrEmpty(titleInfo) ? "" : $": {titleInfo}"),
                MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        public static bool ToBool(this Enum val) {
            return Convert.ToBoolean(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DistanceFrom(this System.Windows.Point pointA, System.Windows.Point pointB) {
            double dx = pointB.X - pointA.X;
            double dy = pointB.Y - pointA.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWithinRadiusOf(this System.Windows.Point pointA, System.Windows.Point pointB, double radius) {
            return pointA.DistanceFrom(pointB) <= radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ClipToByte(this float val) { // TODO: generic?
            if (val < 0) {
                return 0;
            } else if (val > 255) {
                return 255;
            }

            return (byte)val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ClipToByte(this double val) {
            if (val < 0) {
                return 0;
            } else if (val > 255) {
                return 255;
            }

            return (byte)val;
        }
    }
}
