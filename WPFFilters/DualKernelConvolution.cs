using System;
using System.Windows.Media;

namespace WPFFilters {
    public class DualKernelConvolutionFilter : Filter {
        protected sealed override byte[] ApplyFilterHook(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format) {
            byte[] newPixelBuffer = new byte[backBufferStride * bitmapPixelHeight];
            int[,] kernelArray1 = ConvolutionKernel1.KernelArray;
            int[,] kernelArray2 = ConvolutionKernel2.KernelArray;

            // assumes that the kernel sizes are same... :(
            int kernelWidth = ConvolutionKernel1.Width;
            int kernelHeight = ConvolutionKernel1.Height;

            int kernelDenominator1 = ConvolutionKernel1.Denominator == 0 ? 1 : ConvolutionKernel1.Denominator;
            int kernelDenominator2 = ConvolutionKernel2.Denominator == 0 ? 1 : ConvolutionKernel2.Denominator;

            int bytesPerPixel = format.BitsPerPixel / 8;
            int threshold = (int)Threshold.Value;

            for (int y = 0; y < bitmapPixelHeight; ++y) {
                for (int x = 0; x < bitmapPixelWidth; ++x) {
                    int index = y * backBufferStride + x * bytesPerPixel;

                    int sumR1 = 0,
                        sumG1 = 0,
                        sumB1 = 0,

                        sumR2 = 0,
                        sumG2 = 0,
                        sumB2 = 0;

                    // TODO: split this loop in two? agh
                    for (int i = -ConvolutionKernel1.CenterPixelPosY; i < kernelHeight - ConvolutionKernel1.CenterPixelPosY; ++i) {
                        for (int j = -ConvolutionKernel1.CenterPixelPosX; j < kernelWidth - ConvolutionKernel1.CenterPixelPosX; ++j) {
                            // which index is next?
                            int x2 = x + j;
                            int y2 = y + i;
                            if (x2 < 0 || x2 >= bitmapPixelWidth || y2 < 0 || y2 >= bitmapPixelHeight) { // should we skip the index?
                                continue; // (could be simplified?)
                            }

                            // Collect neighbouring pixels
                            int kernelValue1 = kernelArray1[ConvolutionKernel1.CenterPixelPosY + i, ConvolutionKernel1.CenterPixelPosX + j];
                            int kernelValue2 = kernelArray2[ConvolutionKernel1.CenterPixelPosY + i, ConvolutionKernel1.CenterPixelPosX + j];

                            int index2 = y2 * backBufferStride + x2 * bytesPerPixel;

                            sumR1 += pixelBuffer[index2 + 2] * kernelValue1;
                            sumG1 += pixelBuffer[index2 + 1] * kernelValue1;
                            sumB1 += pixelBuffer[index2] * kernelValue1;

                            sumR2 += pixelBuffer[index2 + 2] * kernelValue2;
                            sumG2 += pixelBuffer[index2 + 1] * kernelValue2;
                            sumB2 += pixelBuffer[index2] * kernelValue2;
                        }
                    }

                    sumR1 /= kernelDenominator1;
                    sumG1 /= kernelDenominator1;
                    sumB1 /= kernelDenominator1;

                    sumR2 /= kernelDenominator2;
                    sumG2 /= kernelDenominator2;
                    sumB2 /= kernelDenominator2;

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

        #region stuff
        public KernelDisplay ConvolutionKernel1 { get; private set; }
        public KernelDisplay ConvolutionKernel2 { get; private set; }
        public NamedBoundedValue Threshold { get; private set; }

        public override string VerboseName {
            get => string.Format("{0}{1}",
                BaseName,
                Threshold.Value == 0 ? "" : $" (Threshold: {Math.Round(Threshold.Value, 3)})");
        }
        #endregion

        #region creation
        protected DualKernelConvolutionFilter(IRefreshableContainer refreshableContainer, double threshold = 20)
            : base(refreshableContainer) {
            BaseName = "Dual Kernel";

            Threshold = new NamedBoundedValue(this, "Edge Threshold",
                threshold,
                (0, 255));
        }

        public DualKernelConvolutionFilter(IRefreshableContainer refreshableContainer, int[,] kernelArray1, int[,] kernelArray2, double threshold = 20)
            : this(refreshableContainer, threshold) {
            ConvolutionKernel1 = new KernelDisplay(this, kernelArray1);
            ConvolutionKernel2 = new KernelDisplay(this, kernelArray2);
        }

        public DualKernelConvolutionFilter(IRefreshableContainer refreshableContainer, KernelDisplay convolutionKernel1, KernelDisplay convolutionKernel2, double threshold = 20)
            : this(refreshableContainer, threshold) {
            ConvolutionKernel1 = new KernelDisplay(this, convolutionKernel1);
            ConvolutionKernel2 = new KernelDisplay(this, convolutionKernel2);
        }

        public DualKernelConvolutionFilter(DualKernelConvolutionFilter dualKernelConvolutionFilter)
            : this(dualKernelConvolutionFilter.RefreshableContainer,
                  dualKernelConvolutionFilter.ConvolutionKernel1.Clone() as KernelDisplay,
                  dualKernelConvolutionFilter.ConvolutionKernel2.Clone() as KernelDisplay,
                  dualKernelConvolutionFilter.Threshold.Value) { }

        public override object Clone()
            => new DualKernelConvolutionFilter(this);
        #endregion
    }
}
