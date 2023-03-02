using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Task1Filters {
    public class ConvolutionFilter : Filter {
        public sealed override byte[] applyFilter(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format) {
            byte[] newPixelBuffer = new byte[backBufferStride * bitmapPixelHeight];
            int[,] kernelArray = KernelArray;
            int kernelWidth = KernelWidth;
            int kernelHeight = KernelHeight;
            int kernelDenominator = Denominator == 0 ? 1 : Denominator; // ??
            int bytesPerPixel = (format.BitsPerPixel / 8);

            if (kernelHeight == 0) { // ??
                return pixelBuffer;
            }

            for (int y = 0; y < bitmapPixelHeight; ++y) {
                for (int x = 0; x < bitmapPixelWidth; ++x) {
                    int index = y * backBufferStride + x * bytesPerPixel;

                    // Apply the convolution kernel to the surrounding pixels
                    int sumR = 0,
                        sumG = 0,
                        sumB = 0;
                    int radiusX = CenterPixelPosX - 1;
                    int radiusY = CenterPixelPosY - 1;

                    for (int i = -radiusY; i < kernelHeight - radiusY; ++i) {
                        for (int j = -radiusX; j < kernelWidth - radiusX; ++j) {
                            // which index is next?
                            int x2 = x + j;
                            int y2 = y + i;
                            if (x2 < 0 || x2 >= bitmapPixelWidth || y2 < 0 || y2 >= bitmapPixelHeight) { // should we skip the index?
                                continue; // (could be simplified?)
                            }

                            // Collect neighbouring pixels
                            int kernelValue = kernelArray[radiusY + i, radiusX + j];
                            int index2 = y2 * backBufferStride + x2 * bytesPerPixel;
                            sumR += pixelBuffer[index2 + 2] * kernelValue;
                            sumG += pixelBuffer[index2 + 1] * kernelValue;
                            sumB += pixelBuffer[index2] * kernelValue;
                        }
                    }

                    // Update the pixel data buffer with the new color values
                    newPixelBuffer[index + 2] = (byte)(sumR / kernelDenominator);
                    newPixelBuffer[index + 1] = (byte)(sumG / kernelDenominator);
                    newPixelBuffer[index] = (byte)(sumB / kernelDenominator);

                    // Copy the alpha value from the original pixel
                    newPixelBuffer[index + 3] = pixelBuffer[index + 3];
                }
            }

            return newPixelBuffer;
        }

        // Oof. TODO: do this better
        public class WrappedValue {
            public int Value { get; set; }
        }

        private ObservableCollection<ObservableCollection<WrappedValue>> kernel_;
        public ObservableCollection<ObservableCollection<WrappedValue>> Kernel {
            get => kernel_;
            set {
                kernel_ = value;
                resetCenterPixel();
                notifyKernelShapeChanged();
                OnPropertyChanged(nameof(Denominator));
            }
        }
        public int KernelHeight => Kernel?.Count ?? 0;
        public int KernelWidth => Kernel?.Count > 0 ? Kernel[0].Count : 0;

        public int[,] KernelArray {
            get { // TODO: cache get?
                int[,] kernelArray = new int[KernelHeight, KernelWidth];

                for (int i = 0; i < KernelHeight; ++i) {
                    for (int j = 0; j < KernelWidth; ++j) {
                        kernelArray[i, j] = Kernel[i][j].Value;
                    }
                }

                return kernelArray;
            }

            set {
                int kernelHeight = value.GetLength(0);
                int kernelWidth = value.GetLength(1);

                ObservableCollection<ObservableCollection<WrappedValue>> kernel = new ObservableCollection<ObservableCollection<WrappedValue>>();
                for (int i = 0; i < kernelHeight; ++i) {
                    kernel.Add(new ObservableCollection<WrappedValue>());

                    for (int j = 0; j < kernelWidth; ++j) {
                        kernel[i].Add(new WrappedValue { Value = value[i, j] });
                    }
                }

                Kernel = kernel;
            }
        }

        private bool _denominatorIsLinkedToKernel = true; // recalculate denom every time... ?
        public bool DenominatorIsLinkedToKernel {
            get => _denominatorIsLinkedToKernel;
            set {
                if (_denominatorIsLinkedToKernel != value) { // might be good for UI?
                    _denominatorIsLinkedToKernel = value;
                    OnPropertyChanged(nameof(DenominatorIsLinkedToKernel));
                    OnPropertyChanged(nameof(Denominator));
                }
            }
        }

        public void toggleKernelLink() {
            DenominatorIsLinkedToKernel = !DenominatorIsLinkedToKernel;
        }

        private int _denominator = 1;
        public int Denominator {
            get {
                if (_denominatorIsLinkedToKernel) {
                    _denominator = 0;

                    foreach (var nums in Kernel) {
                        foreach (var num in nums) {
                            _denominator += num.Value;
                        }
                    }
                }

                return _denominator;
            }

            set {
                if (value != 0) {
                    DenominatorIsLinkedToKernel = false;
                    _denominator = value;
                    OnPropertyChanged(nameof(Denominator));
                }
            }
        }

        // eww
        private int _centerPixelPosX;
        public int CenterPixelPosX {
            get => _centerPixelPosX;
            set {
                _centerPixelPosX = value;
                OnPropertyChanged(nameof(CenterPixelPosX));
            }
        }
        private int _centerPixelPosY;
        public int CenterPixelPosY {
            get => _centerPixelPosY;
            set {
                _centerPixelPosY = value;
                OnPropertyChanged(nameof(CenterPixelPosY));
            }
        }

        public void notifyKernelValuesChanged() {
            OnPropertyChanged(nameof(Denominator));
            OnPropertyChanged(nameof(KernelArray));
        }

        public void notifyKernelShapeChanged() {
            notifyKernelValuesChanged();
            OnPropertyChanged(nameof(KernelWidth));
            OnPropertyChanged(nameof(KernelHeight));
        }

        private void resetCenterPixel() {
            _centerPixelPosX = KernelWidth / 2;
            _centerPixelPosY = KernelHeight / 2;

            OnPropertyChanged(nameof(CenterPixelPosX));
            OnPropertyChanged(nameof(CenterPixelPosY));
        }

        public ConvolutionFilter() : base() {
            resetCenterPixel();
        }

        private string name_;
        public override string DisplayName => name_;
        public ConvolutionFilter(string name, int[,] kernelArray) { // tmp?
            name_ = name;
            KernelArray = kernelArray;
        }

        public enum KernelModificationFlags {
            TOP = 0x1,
            BOTTOM,
            LEFT = 0x4,
            RIGHT = 0x8,

            SHOULDADD = 0x10
        }

        public void modifyKernelShape(KernelModificationFlags kernelModificationFlags) {
            Console.WriteLine(kernelModificationFlags); // tmp

            if (kernelModificationFlags.HasFlag(KernelModificationFlags.SHOULDADD)) {
                if ((kernelModificationFlags & (KernelModificationFlags.TOP | KernelModificationFlags.BOTTOM)).toBool()) {
                    var newVals = new ObservableCollection<WrappedValue>();
                    for (int i = 0; i < Math.Max(KernelWidth, 1); ++i)
                        newVals.Add(new WrappedValue { Value = 0 });

                    if ((kernelModificationFlags & KernelModificationFlags.TOP).toBool()) {
                        Kernel.Insert(0, newVals);
                    }

                    if ((kernelModificationFlags & KernelModificationFlags.BOTTOM).toBool()) {
                        Kernel.Add(newVals);
                    }
                }

                if ((kernelModificationFlags & (KernelModificationFlags.LEFT | KernelModificationFlags.RIGHT)).toBool()) {
                    if ((kernelModificationFlags & KernelModificationFlags.LEFT).toBool()) {
                        foreach (var row in Kernel) {
                            row.Insert(0, new WrappedValue { Value = 0 });
                        }
                    }

                    if ((kernelModificationFlags & KernelModificationFlags.RIGHT).toBool()) {
                        foreach (var row in Kernel) {
                            row.Add(new WrappedValue { Value = 0 });
                        }
                    }
                }
            } else { // should remove
                if (KernelHeight == 0) {
                    goto Done; // xdd?
                }

                if ((kernelModificationFlags & KernelModificationFlags.TOP).toBool()) {
                    Kernel.RemoveAt(0);
                }

                if ((kernelModificationFlags & KernelModificationFlags.BOTTOM).toBool()) {
                    Kernel.RemoveAt(KernelHeight - 1);
                }

                if ((kernelModificationFlags & KernelModificationFlags.LEFT).toBool()) {
                    foreach (var row in Kernel) {
                        row.RemoveAt(0);
                    }
                }

                if ((kernelModificationFlags & KernelModificationFlags.RIGHT).toBool()) {
                    foreach (var row in Kernel) {
                        row.RemoveAt(KernelWidth - 1);
                    }
                }
            }

        Done:
            resetCenterPixel();
            notifyKernelShapeChanged();
        }

        public override object Clone() {
            ConvolutionFilter clone = MemberwiseClone() as ConvolutionFilter;
            clone.KernelArray = KernelArray;
            return clone;
        }
    }

    [ValueConversion(typeof(bool), typeof(string))]
    internal class ConvolutionFilterDenominatorIsLinkedToKernelToString : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value ? "Linked" : "Unlinked";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
