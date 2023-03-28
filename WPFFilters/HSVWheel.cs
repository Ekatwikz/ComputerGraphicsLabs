﻿using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WPFFilters {
    public class HSVWheel : Filter {
        protected override byte[] ApplyFilterHook(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format) {
            int size = (int)Size.Value;

            for (int y = 0; y < bitmapPixelHeight; ++y) {
                for (int x = 0; x < bitmapPixelWidth; ++x) {
                    int index = y * backBufferStride + x * (format.BitsPerPixel / 8);

                    int dx = x - bitmapPixelWidth / 2;
                    int dy = y - bitmapPixelHeight / 2;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance <= size) {
                        double hue = 360.0 * (Math.Atan2(dy, dx) / (2 * Math.PI) + 0.5);
                        double saturation = distance / size;
                        double value = 1.0;

                        HSVToRGB(hue, saturation, value, out pixelBuffer[index + 2], out pixelBuffer[index + 1], out pixelBuffer[index + 0]);
                    }
                }
            }

            return pixelBuffer;
        }

        private static void HSVToRGB(double hue, double saturation, double value, out byte r, out byte g, out byte b) {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);
            double p = value * (1 - saturation);
            double q = value * (1 - f * saturation);
            double t = value * (1 - (1 - f) * saturation);

            switch (hi) {
                case 0:
                    r = Convert.ToByte(value * 255);
                    g = Convert.ToByte(t * 255);
                    b = Convert.ToByte(p * 255);
                    break;
                case 1:
                    r = Convert.ToByte(q * 255);
                    g = Convert.ToByte(value * 255);
                    b = Convert.ToByte(p * 255);
                    break;
                case 2:
                    r = Convert.ToByte(p * 255);
                    g = Convert.ToByte(value * 255);
                    b = Convert.ToByte(t * 255);
                    break;
                case 3:
                    r = Convert.ToByte(p * 255);
                    g = Convert.ToByte(q * 255);
                    b = Convert.ToByte(value * 255);
                    break;
                case 4:
                    r = Convert.ToByte(t * 255);
                    g = Convert.ToByte(p * 255);
                    b = Convert.ToByte(value * 255);
                    break;
                default:
                    r = Convert.ToByte(value * 255);
                    g = Convert.ToByte(p * 255);
                    b = Convert.ToByte(q * 255);
                    break;
            }
        }

        public NamedBoundedValue Size { get; private set; }
        public override string VerboseName => $"{BaseName} ({Size.VerboseName})";

        #region creation
        public HSVWheel(IRefreshableContainer refreshableContainer, double value = 300, string name = "HSV Wheel Overlay")
            : base(refreshableContainer) {
            BaseName = name;
            Size = new NamedBoundedValue(this, "Size",
                value,
                (1, 1000));
        }

        public HSVWheel(HSVWheel hSVWheel)
            : this(hSVWheel.RefreshableContainer, hSVWheel.Size.Value, hSVWheel.BaseName) { }

        public override object Clone()
            => new HSVWheel(this);
        #endregion
    }
}
