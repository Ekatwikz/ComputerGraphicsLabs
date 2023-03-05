using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Task1Filters {
    internal static class MyExtensions {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        public static BitmapSource ToBitmapSource(this Bitmap bitmap) {
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

        public static BitmapSource ToBitmapSource(this object dataObject) {
            if (dataObject is WriteableBitmap writeableBitmap) {
                throw new Exception("??");
            }

            if (dataObject is BitmapSource bitmapSource) { // ??
                return bitmapSource;
            }

            throw new ArgumentException("idk?");
        }

        public static BitmapSource ToBitmapSource(this IDataObject dataObject) {
            return dataObject.GetData(DataFormats.Bitmap).ToBitmapSource();
        }

        public static bool ToBool(this Enum val) {
            return Convert.ToBoolean(val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // worth a shot?
        public static byte ClipToByte(this int val) {
            if (val < 0) {
                return 0;
            } else if (val > 255) {
                return 255;
            }

            return (byte)val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ClipToByte(this double val) { // TODO: generic?
            if (val < 0) {
                return 0;
            } else if (val > 255) {
                return 255;
            }

            return (byte)val;
        }

        public static T FindAncestor<T>(this DependencyObject current) where T : DependencyObject {
            current = VisualTreeHelper.GetParent(current);

            while (current != null && !(current is T)) {
                current = VisualTreeHelper.GetParent(current);
            }

            return current as T;
        }
    }
}
