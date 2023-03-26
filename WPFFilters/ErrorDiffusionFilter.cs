using System.Windows.Media;

namespace WPFFilters {
    public class ErrorDiffusionFilter : Filter {
        protected sealed override byte[] ApplyFilterHook(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format) {
            #region stuff
            int kernelWidth = DiffusionWeightsKernel.Width;
            int kernelHeight = DiffusionWeightsKernel.Height;

            if (kernelHeight == 0) { // ??
                return pixelBuffer;
            }

            byte[] newPixelBuffer = new byte[backBufferStride * bitmapPixelHeight];
            pixelBuffer.CopyTo(newPixelBuffer, 0);

            int[,] kernelArray = DiffusionWeightsKernel.KernelArray;
            int kernelDenominator = DiffusionWeightsKernel.Denominator == 0 ? 1 : DiffusionWeightsKernel.Denominator; // ??
            int bytesPerPixel = format.BitsPerPixel / 8;
            #endregion

            for (int y = 0; y < bitmapPixelHeight; ++y) {
                for (int x = 0; x < bitmapPixelWidth; ++x) {
                    int index = y * backBufferStride + x * bytesPerPixel;

                    byte inputR = newPixelBuffer[index + 2],
                        inputG = newPixelBuffer[index + 1],
                        inputB = newPixelBuffer[index];

                    // Calculate error for each channel
                    int errorR = inputR - (newPixelBuffer[index + 2] = QuantizeFilter.RedFunctionDisplay.LookupTable[inputR]),
                        errorG = inputG - (newPixelBuffer[index + 1] = QuantizeFilter.GreenFunctionDisplay.LookupTable[inputG]),
                        errorB = inputB - (newPixelBuffer[index] = QuantizeFilter.BlueFunctionDisplay.LookupTable[inputB]);

                    // distribute error
                    for (int i = -DiffusionWeightsKernel.CenterPixelPosY; i < kernelHeight - DiffusionWeightsKernel.CenterPixelPosY; ++i) {
                        for (int j = -DiffusionWeightsKernel.CenterPixelPosX; j < kernelWidth - DiffusionWeightsKernel.CenterPixelPosX; ++j) {
                            // which index is next?
                            int x2 = x + j;
                            int y2 = y + i;
                            if (x2 < 0 || x2 >= bitmapPixelWidth || y2 < 0 || y2 >= bitmapPixelHeight) { // should we skip the index?
                                continue; // (could be simplified?)
                            }

                            // Distribute error to pixel
                            int kernelValue = kernelArray[DiffusionWeightsKernel.CenterPixelPosY + i, DiffusionWeightsKernel.CenterPixelPosX + j];
                            int index2 = y2 * backBufferStride + x2 * bytesPerPixel;

                            // I think not += b/c overflow ?
                            newPixelBuffer[index2 + 2] = (newPixelBuffer[index2 + 2] + errorR * kernelValue * 1.0 / kernelDenominator).ClipToByte();
                            newPixelBuffer[index2 + 1] = (newPixelBuffer[index2 + 1] + errorG * kernelValue * 1.0 / kernelDenominator).ClipToByte();
                            newPixelBuffer[index2] = (newPixelBuffer[index2] + errorB * kernelValue * 1.0 / kernelDenominator).ClipToByte();
                        }
                    }
                }
            }

            return newPixelBuffer;
        }

        public UniformColorQuantize QuantizeFilter { get; set; }
        public KernelDisplay DiffusionWeightsKernel { get; private set; }

        public override string VerboseName
            => $"{BaseName}: {QuantizeFilter.Info} {DiffusionWeightsKernel.Info}"; // TODO: fix me :(

        #region creation
        private ErrorDiffusionFilter(IRefreshableContainer refreshableContainer, string baseName)
            : base(refreshableContainer) {
            BaseName = baseName;
        }

        public ErrorDiffusionFilter(IRefreshableContainer refreshableContainer, string name, int[,] diffusionWeights, int? denominator = null, UniformColorQuantize quantizeFilter = null)
            : this(refreshableContainer, $"{name} Diffusion") {
            QuantizeFilter = quantizeFilter == null ? new UniformColorQuantize(this) : new UniformColorQuantize(this, quantizeFilter);
            DiffusionWeightsKernel = new KernelDisplay(this, diffusionWeights, denominator);
        }

        // yuck.
        public ErrorDiffusionFilter(IRefreshableContainer refreshableContainer, string name, UniformColorQuantize quantizeFilter, int[,] diffusionWeights)
            : this(refreshableContainer, name, diffusionWeights, null, quantizeFilter) { }

        public ErrorDiffusionFilter(IRefreshableContainer refreshableContainer, string baseName, UniformColorQuantize quantizeFilter, KernelDisplay diffusionWeightsKernel)
            : this(refreshableContainer, baseName) {
            QuantizeFilter = new UniformColorQuantize(this, quantizeFilter);
            DiffusionWeightsKernel = new KernelDisplay(this, diffusionWeightsKernel);
        }

        public ErrorDiffusionFilter(ErrorDiffusionFilter errorDiffusionFilter)
            : this(errorDiffusionFilter.RefreshableContainer,
                  errorDiffusionFilter.BaseName,
                  errorDiffusionFilter.QuantizeFilter,
                  errorDiffusionFilter.DiffusionWeightsKernel) { }

        public override object Clone()
            => new ErrorDiffusionFilter(this);
        #endregion
    }
}
