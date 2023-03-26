using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WPFFilters {
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
    }
}
