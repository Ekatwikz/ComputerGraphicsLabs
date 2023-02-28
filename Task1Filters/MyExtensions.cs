using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Task1Filters {
    internal static class MyExtensions {
        public static Bitmap toBitmap(this WriteableBitmap writeableBitmap) {
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
            if (dataObject is BitmapSource) {
                return dataObject as BitmapSource;
            }

            if (dataObject is WriteableBitmap) {
                return ((WriteableBitmap)dataObject).toBitmap().toBitmapSource();
            }

            return null;
        }

        public static BitmapSource toBitmapSource(this IDataObject dataObject) {
            return dataObject.GetData(DataFormats.Bitmap).toBitmapSource();
        }
    }
}
