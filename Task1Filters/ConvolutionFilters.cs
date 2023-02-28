using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace Task1Filters {
    public abstract class ConvolutionFilter : Filter {
        public sealed override byte[] applyFilter(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride) {
            byte[] bufferClone = pixelBuffer.ToArray();

            for (int y = 0; y < bitmapPixelHeight; ++y) {
                for (int x = 0; x < bitmapPixelWidth; ++x) {
                    int index = y * backBufferStride + 4 * x;

                    // TODO
                }
            }

            return pixelBuffer;
        }

        // Oof. Ouch.
        public class WrappedValue : INotifyPropertyChanged {
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName) {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            private int value_;
            public int Value {
                get { return value_; }
                set {
                    value_ = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public abstract ObservableCollection<ObservableCollection<WrappedValue>> Kernel { get; }
        public int KernelHeight => Kernel.Count;
        public int KernelWidth => Kernel.Count > 0 ? Kernel[0].Count : 0;

        private int denominator_;
        private bool linkedToKernel_ = true; // recalculate every time... ?
        public int Denominator {
            get {
                if (linkedToKernel_) {
                    denominator_ = 0;

                    foreach (var nums in Kernel) {
                        foreach (var num in nums) {
                            denominator_ += num.Value;
                        }
                    }
                }

                return denominator_;
            }

            set {
                linkedToKernel_ = false;
                denominator_ = value;
                OnPropertyChanged(nameof(Denominator));
            }
        }

        protected abstract (int, int) CenterPixelPos { get; }
    }

    public class BoxBlurFilter : ConvolutionFilter {
        public override string DisplayName => "BoxBlur";

        private readonly ObservableCollection<ObservableCollection<WrappedValue>> kernel_
            = new ObservableCollection<ObservableCollection<WrappedValue>> { new ObservableCollection<WrappedValue> { new WrappedValue { Value = 3 }, new WrappedValue { Value = 1 } } };

        public override ObservableCollection<ObservableCollection<WrappedValue>> Kernel => kernel_;

        protected override (int, int) CenterPixelPos => (KernelWidth, KernelHeight);
    }
}
