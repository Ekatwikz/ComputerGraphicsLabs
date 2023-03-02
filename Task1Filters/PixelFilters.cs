using System;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Task1Filters {
    public abstract class PixelFilter : Filter {
        public sealed override byte[] applyFilter(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format) {
            for (int y = 0; y < bitmapPixelHeight; ++y) {
                for (int x = 0; x < bitmapPixelWidth; ++x) {
                    int index = y * backBufferStride + x * (format.BitsPerPixel / 8);

                    // 0: Blue, 1: Green, 2: Red?
                    for (int i = 0; i < 3; ++i) {
                        pixelBuffer[index + i] = byteFilterHook(pixelBuffer[index + i]);
                    }
                }
            }

            return pixelBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // will this work?
        protected abstract byte byteFilterHook(byte inputByte);

        public override object Clone() {
            return MemberwiseClone();
        }
    }

    public class InverseFilter : PixelFilter {
        public override string DisplayName => "Inverse";

        protected override byte byteFilterHook(byte inputByte) => (byte)(255 - inputByte);
    }

    public class GammaCorrectionFilter : PixelFilter {
        private double _gammaLevel = 1;
        public double GammaLevel {
            get => _gammaLevel;

            set {
                _gammaLevel = value;
                OnPropertyChanged("GammaLevel");
                OnPropertyChanged("DisplayName");
            }
        }

        private string baseName;
        public override string DisplayName
            => $"{baseName} ({Math.Round(GammaLevel, 3)})";

        // TODO: make this faster?
        protected override byte byteFilterHook(byte inputByte)
            => Convert.ToByte(255D * Math.Pow(inputByte / 255D, GammaLevel));

        public GammaCorrectionFilter() {
            baseName = "Gamma Correction";
        }

        public GammaCorrectionFilter(string baseName) {
            this.baseName = baseName;
        }
    }
}
