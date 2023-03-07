using System;
using System.Windows.Media;

namespace Task1Filters {
    public class DualKernelConvolutionFilter : Filter {
        protected sealed override byte[] ApplyFilterHook(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format) {
            byte[] newPixelBuffer = new byte[backBufferStride * bitmapPixelHeight];
            int[,] kernelArray1 = new int[,] {
                    { 0, -1, 0 },
                    { 0, 1, 0 },
                    { 0, 0, 0 },
                };
            int[,] kernelArray2 = new int[,] {
                    { 0, 0 , 0 },
                    { -1, 1 , 0 },
                    { 0, 0 , 0 },
                };

            // TODO: maybe not hardcode these?
            int kernelWidth = 3;
            int kernelHeight = 3;
            int kernelDenominator = 1;

            int bytesPerPixel = format.BitsPerPixel / 8;
            int threshold = (int)Threshold.Value;

            for (int y = 0; y < bitmapPixelHeight; ++y) {
                for (int x = 0; x < bitmapPixelWidth; ++x) {
                    int index = y * backBufferStride + x * bytesPerPixel;

                    // Apply the convolution kernel to the surrounding pixels
                    int sumR1 = 0,
                        sumG1 = 0,
                        sumB1 = 0,

                        sumR2 = 0,
                        sumG2 = 0,
                        sumB2 = 0;
                    int radiusX = 1;
                    int radiusY = 1;

                    for (int i = -radiusY; i < kernelHeight - radiusY; ++i) {
                        for (int j = -radiusX; j < kernelWidth - radiusX; ++j) {
                            // which index is next?
                            int x2 = x + j;
                            int y2 = y + i;
                            if (x2 < 0 || x2 >= bitmapPixelWidth || y2 < 0 || y2 >= bitmapPixelHeight) { // should we skip the index?
                                continue; // (could be simplified?)
                            }

                            // Collect neighbouring pixels
                            int kernelValue1 = kernelArray1[radiusY + i, radiusX + j];
                            int kernelValue2 = kernelArray2[radiusY + i, radiusX + j];

                            int index2 = y2 * backBufferStride + x2 * bytesPerPixel;

                            sumR1 += pixelBuffer[index2 + 2] * kernelValue1;
                            sumG1 += pixelBuffer[index2 + 1] * kernelValue1;
                            sumB1 += pixelBuffer[index2] * kernelValue1;

                            sumR2 += pixelBuffer[index2 + 2] * kernelValue2;
                            sumG2 += pixelBuffer[index2 + 1] * kernelValue2;
                            sumB2 += pixelBuffer[index2] * kernelValue2;
                        }
                    }

                    sumR1 /= kernelDenominator;
                    sumG1 /= kernelDenominator;
                    sumB1 /= kernelDenominator;

                    sumR2 /= kernelDenominator;
                    sumG2 /= kernelDenominator;
                    sumB2 /= kernelDenominator;

                    // Update the pixel data buffer with the new color values
                    if (Math.Abs(sumR1) > threshold || Math.Abs(sumG1) > threshold || Math.Abs(sumB1) > threshold
                        || Math.Abs(sumR2) > threshold || Math.Abs(sumG2) > threshold || Math.Abs(sumB2) > threshold) {
                        newPixelBuffer[index + 2] = 255;
                        newPixelBuffer[index + 1] = 255;
                        newPixelBuffer[index + 0] = 255;
                    } else {
                        newPixelBuffer[index + 2] = 0;
                        newPixelBuffer[index + 1] = 0;
                        newPixelBuffer[index + 0] = 0;
                    }

                    // Copy the alpha value from the original pixel
                    newPixelBuffer[index + 3] = pixelBuffer[index + 3];
                }
            }

            return newPixelBuffer;
        }

        private NamedBoundedFilterParam _threshold;
        public NamedBoundedFilterParam Threshold {
            get => _threshold;
            set {
                _threshold = value;
                OnPropertyChanged(nameof(VerboseName));
            }
        }

        public void NotifyfilterParameterChanged() {
            OnPropertyChanged(nameof(VerboseName));
            OnPropertyChanged(nameof(Threshold));
        }

        public DualKernelConvolutionFilter(int threshold = 50) {
            BaseName = "Dual Kernel with Threshold";
            Threshold = new NamedBoundedFilterParam("Edge Threshold",
                threshold,
                (0, 255));
        }

        public override string VerboseName {
            get {
                string extraInfo = "";

                if (Threshold.Value != 0) {
                    extraInfo = $" (Threshold: {Math.Round(Threshold.Value, 3)})";
                }

                return $"{BaseName}{extraInfo}";
            }
        }

        public override object Clone() {
            DualKernelConvolutionFilter clone = MemberwiseClone() as DualKernelConvolutionFilter;
            clone.Threshold = (NamedBoundedFilterParam)Threshold.Clone();
            return clone;
        }
    }
}
