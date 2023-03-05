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
            int bytesPerPixel = format.BitsPerPixel / 8;
            int offset = (int)Offset.Value;

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
                    int radiusX = CenterPixelPosX;
                    int radiusY = CenterPixelPosY;

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
                    newPixelBuffer[index + 2] = (offset + sumR / kernelDenominator).clipToByte();
                    newPixelBuffer[index + 1] = (offset + sumG / kernelDenominator).clipToByte();
                    newPixelBuffer[index] = (offset + sumB / kernelDenominator).clipToByte();

                    // Copy the alpha value from the original pixel
                    newPixelBuffer[index + 3] = pixelBuffer[index + 3];
                }
            }

            return newPixelBuffer;
        }

        // Oof. TODO: can this be done better?
        public class WrappedValue {
            public int Value { get; set; }

            public WrappedValue(int value = 0) {
                Value = value;
            }
        }

        private ObservableCollection<ObservableCollection<WrappedValue>> kernel_;
        public ObservableCollection<ObservableCollection<WrappedValue>> Kernel {
            get => kernel_;
            protected set {
                kernel_ = value;
                ResetCenterPixelIfLinked();
                // notifyKernelShapeChanged(); // ?
                OnPropertyChanged(nameof(Denominator));
                OnPropertyChanged(nameof(Kernel));
            }
        }

        public int KernelHeight => Kernel?.Count ?? 0;
        public int KernelWidth => Kernel?.Count > 0 ? Kernel[0].Count : 0;

        public int[,] KernelArray {
            get { // TODO: cache get/set?
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
                        kernel[i].Add(new WrappedValue(value[i, j]));
                    }
                }

                Kernel = kernel;
            }
        }

        private bool KernelIsUnModified { get; set; } = true; // for UI?

        private bool _denominatorIsLinkedToKernel = true; // recalculate denom every time... ?
        public bool DenominatorIsLinkedToKernel {
            get => _denominatorIsLinkedToKernel;
            set {
                if (_denominatorIsLinkedToKernel != value) { // might be good for UI?
                    _denominatorIsLinkedToKernel = value;
                    OnPropertyChanged(nameof(DenominatorIsLinkedToKernel));
                    OnPropertyChanged(nameof(Denominator));
                    OnPropertyChanged(nameof(VerboseName));
                }
            }
        }

        public void ToggleDenominatorLink() {
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
                CenterPixelIsLinkedToKernel = false;
                _centerPixelPosX = value;
                OnPropertyChanged(nameof(CenterPixelPosX));
            }
        }
        private int _centerPixelPosY;
        public int CenterPixelPosY {
            get => _centerPixelPosY;
            set {
                CenterPixelIsLinkedToKernel = false;
                _centerPixelPosY = value;
                OnPropertyChanged(nameof(CenterPixelPosY));
            }
        }

        private bool _centerPixelIsLinkedToKernel = true;
        public bool CenterPixelIsLinkedToKernel {
            get => _centerPixelIsLinkedToKernel;
            set {
                if (_centerPixelIsLinkedToKernel != value) {
                    _centerPixelIsLinkedToKernel = value;
                    OnPropertyChanged(nameof(CenterPixelIsLinkedToKernel));
                    OnPropertyChanged(nameof(CenterPixelPosX));
                    OnPropertyChanged(nameof(CenterPixelPosY));
                    OnPropertyChanged(nameof(VerboseName));
                }

                ResetCenterPixelIfLinked();
            }
        }

        public void ToggleCenterPixelLink() {
            CenterPixelIsLinkedToKernel = !CenterPixelIsLinkedToKernel;
        }

        public void NotifyKernelValuesChanged() {
            KernelIsUnModified = false;
            OnPropertyChanged(nameof(VerboseName));
            OnPropertyChanged(nameof(Denominator));
            OnPropertyChanged(nameof(KernelArray));
        }

        public void NotifyKernelShapeChanged() {
            NotifyKernelValuesChanged();
            OnPropertyChanged(nameof(KernelWidth));
            OnPropertyChanged(nameof(KernelHeight));
        }

        private void ResetCenterPixelIfLinked() {
            if (CenterPixelIsLinkedToKernel) {
                _centerPixelPosX = KernelWidth / 2;
                _centerPixelPosY = KernelHeight / 2;

                OnPropertyChanged(nameof(CenterPixelPosX));
                OnPropertyChanged(nameof(CenterPixelPosY));
            }
        }

        public override string VerboseName {
            get {
                string extraInfo = "";

                if (Offset.Value != 0) {
                    extraInfo = $" (Offset: {Math.Round(Offset.Value, 3)})";
                } else if (!DenominatorIsLinkedToKernel || !KernelIsUnModified || !CenterPixelIsLinkedToKernel) {
                    extraInfo = " (Tweaked)";
                }

                return $"{BaseName}{extraInfo}";
            }
        }

        public enum KernelModificationFlags {
            TOP = 0x1,
            BOTTOM,
            LEFT = 0x4,
            RIGHT = 0x8,

            SHOULDADD = 0x10
        }

        public void ModifyKernelShape(KernelModificationFlags kernelModificationFlags) {
            if ((kernelModificationFlags & KernelModificationFlags.SHOULDADD).toBool()) {
                if ((kernelModificationFlags & (KernelModificationFlags.TOP | KernelModificationFlags.BOTTOM)).toBool()) {
                    var newRow = new ObservableCollection<WrappedValue>();
                    for (int i = 0; i < Math.Max(KernelWidth, 1); ++i)
                        newRow.Add(new WrappedValue());

                    if ((kernelModificationFlags & KernelModificationFlags.TOP).toBool()) {
                        Kernel.Insert(0, newRow);
                    }

                    if ((kernelModificationFlags & KernelModificationFlags.BOTTOM).toBool()) {
                        Kernel.Add(newRow);
                    }
                }

                if ((kernelModificationFlags & (KernelModificationFlags.LEFT | KernelModificationFlags.RIGHT)).toBool()) {
                    if (KernelWidth == 0) {
                        KernelArray = new int[,] { { 0 } };
                        goto Done;
                    }

                    if ((kernelModificationFlags & KernelModificationFlags.LEFT).toBool()) {
                        foreach (var row in Kernel) {
                            row.Insert(0, new WrappedValue());
                        }
                    }

                    if ((kernelModificationFlags & KernelModificationFlags.RIGHT).toBool()) {
                        foreach (var row in Kernel) {
                            row.Add(new WrappedValue());
                        }
                    }
                }
            } else { // ShouldRemove
                if (KernelHeight == 0) {
                    goto Done;
                }

                if ((kernelModificationFlags & KernelModificationFlags.TOP).toBool()) {
                    Kernel.RemoveAt(0);
                }

                if ((kernelModificationFlags & KernelModificationFlags.BOTTOM).toBool()) {
                    Kernel.RemoveAt(KernelHeight - 1);
                }

                if ((kernelModificationFlags & (KernelModificationFlags.LEFT | KernelModificationFlags.RIGHT)).toBool()
                    && KernelWidth == 1) {
                    Kernel.Clear();
                    goto Done;
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
            ResetCenterPixelIfLinked();
            NotifyKernelShapeChanged();
        }

        private NamedBoundedFilterParam _offset;
        public NamedBoundedFilterParam Offset {
            get => _offset;
            set {
                _offset = value;
                OnPropertyChanged(nameof(VerboseName));
            }
        }
        public void NotifyConvolutionOffsetChanged() {
            OnPropertyChanged(nameof(Offset));
            OnPropertyChanged(nameof(VerboseName));
        }

        public ConvolutionFilter(string name, int[,] kernelArray, int offset = 0) {
            BaseName = name;
            KernelArray = kernelArray;
            Offset = new NamedBoundedFilterParam(nameof(Offset),
                offset,
                (-255, 255));
        }

        public override object Clone() {
            ConvolutionFilter clone = MemberwiseClone() as ConvolutionFilter;
            clone.KernelArray = KernelArray;
            clone.KernelIsUnModified = true;
            clone.Offset = (NamedBoundedFilterParam)Offset.Clone();
            return clone;
        }
    }

    [ValueConversion(typeof(bool), typeof(string))] // TODO: remove lol
    internal class IsLinkedBooleanToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value ? "Linked" : "Unlinked";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
