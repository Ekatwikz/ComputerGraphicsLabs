using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Task1Filters {
    public abstract class Filter : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged; // Unnecessary for some, but whatever
        protected void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public abstract string DisplayName { get; }

        public abstract byte[] applyFilter(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride);
    }

    public abstract class PixelFilter : Filter {
        public sealed override byte[] applyFilter(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride) {
            for (int y = 0; y < bitmapPixelHeight; ++y) {
                for (int x = 0; x < bitmapPixelWidth; ++x) {
                    int index = y * backBufferStride + 4 * x;

                    // 0: Blue, 1: Green, 2: Red?
                    for (int i = 0; i < 3; ++i) {
                        pixelBuffer[index + i] = byteFilterHook(pixelBuffer[index + i]);
                    }
                }
            }

            return pixelBuffer;
        }

        protected abstract byte byteFilterHook(byte inputByte);
    }

    public class InverseFilter : PixelFilter {
        public override string DisplayName => "Inverse";

        protected override byte byteFilterHook(byte inputByte) => (byte)(255 - inputByte);
    }

    public class GammaCorrectionFilter : PixelFilter {
        private double gammaLevel_ = 1;
        public double GammaLevel {
            get {
                return gammaLevel_;
            }

            set {
                gammaLevel_ = value;
                OnPropertyChanged("GammaLevel");
                OnPropertyChanged("DisplayName");
            }
        }

        private string baseName;
        public override string DisplayName
            => $"{baseName} ({Math.Round(GammaLevel, 3)})";

        protected override byte byteFilterHook(byte inputByte)
            => Convert.ToByte(255D * Math.Pow(inputByte / 255D, GammaLevel));

        public GammaCorrectionFilter() {
            baseName = "Gamma Correction";
        }

        public GammaCorrectionFilter(string baseName) {
            this.baseName = baseName;
        }
    }

    public class ObservableFilterCollection : ObservableCollection<Filter> { }
}
