using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Task1Filters {
    internal static class MyExtensions {
        public static Bitmap toBitmap(this WriteableBitmap writeableBitmap) { // ??
            Bitmap bitmap;

            using (MemoryStream outStream = new MemoryStream()) {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                encoder.Save(outStream);
                bitmap = new Bitmap(outStream);
            }

            return bitmap;
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        public static BitmapSource toBitmapSource(this Bitmap bitmap) {
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

        public static BitmapSource toBitmapSource(this object dataObject) {
            if (dataObject is BitmapSource bitmapSource) { // ??
                return bitmapSource;
            }

            if (dataObject is WriteableBitmap writeableBitmap) {
                return writeableBitmap.toBitmap().toBitmapSource();
            }

            throw new ArgumentException("idk?");
        }

        public static BitmapSource toBitmapSource(this IDataObject dataObject) {
            return dataObject.GetData(DataFormats.Bitmap).toBitmapSource();
        }

        public static bool toBool(this Enum val) {
            return Convert.ToBoolean(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // worth a shot?
        public static byte clipToByte(this int val) {
            if (val < 0) {
                return 0;
            } else if (val > 255) {
                return 255;
            }

            return (byte)val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte clipToByte(this double val) { // TODO: generic?
            if (val < 0) {
                return 0;
            } else if (val > 255) {
                return 255;
            }

            return (byte)val;
        }
    }
}
