﻿using System.Windows.Media;

namespace Task1Filters {
    public class ConvolutionFilter : Filter {
        protected sealed override byte[] ApplyFilterHook(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format) {
            byte[] newPixelBuffer = new byte[backBufferStride * bitmapPixelHeight];
            int[,] kernelArray = ConvolutionKernel.KernelArray;
            int kernelWidth = ConvolutionKernel.KernelWidth;
            int kernelHeight = ConvolutionKernel.KernelHeight;
            int kernelDenominator = ConvolutionKernel.Denominator == 0 ? 1 : ConvolutionKernel.Denominator; // ??
            int bytesPerPixel = format.BitsPerPixel / 8;
            int offset = (int)ConvolutionKernel.Offset.Value;

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

                    for (int i = -ConvolutionKernel.CenterPixelPosY; i < kernelHeight - ConvolutionKernel.CenterPixelPosY; ++i) {
                        for (int j = -ConvolutionKernel.CenterPixelPosX; j < kernelWidth - ConvolutionKernel.CenterPixelPosX; ++j) {
                            // which index is next?
                            int x2 = x + j;
                            int y2 = y + i;
                            if (x2 < 0 || x2 >= bitmapPixelWidth || y2 < 0 || y2 >= bitmapPixelHeight) { // should we skip the index?
                                continue; // (could be simplified?)
                            }

                            // Collect neighbouring pixels
                            int kernelValue = kernelArray[ConvolutionKernel.CenterPixelPosY + i, ConvolutionKernel.CenterPixelPosX + j];
                            int index2 = y2 * backBufferStride + x2 * bytesPerPixel;
                            sumR += pixelBuffer[index2 + 2] * kernelValue;
                            sumG += pixelBuffer[index2 + 1] * kernelValue;
                            sumB += pixelBuffer[index2] * kernelValue;
                        }
                    }

                    // Update the pixel data buffer with the new color values
                    newPixelBuffer[index + 2] = (offset + sumR / kernelDenominator).ClipToByte();
                    newPixelBuffer[index + 1] = (offset + sumG / kernelDenominator).ClipToByte();
                    newPixelBuffer[index] = (offset + sumB / kernelDenominator).ClipToByte();

                    // Copy alpha?
                    newPixelBuffer[index + 3] = pixelBuffer[index + 3];
                }
            }

            return newPixelBuffer;
        }

        public Kernel ConvolutionKernel { get; private set; }
        public override string VerboseName => string.Format("{0}{1}",
            BaseName,
            ConvolutionKernel.Info == "" ? "" : $" ({ConvolutionKernel.VerboseName})");

        public ConvolutionFilter(IRefreshableContainer refreshableContainer, string name, int[,] kernelArray, int offset = 0)
            : base(refreshableContainer) {
            BaseName = name;
            ConvolutionKernel = new Kernel(this, kernelArray, offset);
        }

        public override object Clone() {
            // TODO: remove me
            ConvolutionFilter clone = MemberwiseClone() as ConvolutionFilter;
            clone.ConvolutionKernel = ConvolutionKernel.Clone() as Kernel;
            clone.ConvolutionKernel.RefreshableContainer = clone;

            return clone;
        }
    }
}
