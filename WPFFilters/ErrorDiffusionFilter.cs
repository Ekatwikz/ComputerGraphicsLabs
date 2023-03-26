using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WPFFilters {
    public class ErrorDiffusionFilter : UniformColorQuantize {
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
                    int errorR = inputR - (newPixelBuffer[index + 2] = RedSubdivisions.LookupTable[inputR]),
                        errorG = inputG - (newPixelBuffer[index + 1] = GreenSubdivisions.LookupTable[inputG]),
                        errorB = inputB - (newPixelBuffer[index] = BlueSubdivisions.LookupTable[inputB]);

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

        public KernelDisplay DiffusionWeightsKernel { get; private set; }

        #region creation
        public ErrorDiffusionFilter(IRefreshableContainer refreshableContainer, string baseName, NamedBoundedValue redSubdivisionsParameter, NamedBoundedValue greenSubdivisionsParameter, NamedBoundedValue blueSubdivisionsParameter, int[,] diffusionWeights, int? denominator = null)
            : base(refreshableContainer,
                  baseName,
                  redSubdivisionsParameter,
                  blueSubdivisionsParameter,
                  greenSubdivisionsParameter
                  ) {
            DiffusionWeightsKernel = new KernelDisplay(this, diffusionWeights, denominator);
        }

        public ErrorDiffusionFilter(IRefreshableContainer refreshableContainer, string baseName, NamedBoundedValue redSubdivisionsParameter, NamedBoundedValue greenSubdivisionsParameter, NamedBoundedValue blueSubdivisionsParameter, KernelDisplay diffusionWeightsDisplay)
            : base(refreshableContainer,
                  baseName,
                  redSubdivisionsParameter,
                  blueSubdivisionsParameter,
                  greenSubdivisionsParameter
                  ) {
            DiffusionWeightsKernel = new KernelDisplay(this, diffusionWeightsDisplay);
        }

        public ErrorDiffusionFilter(IRefreshableContainer refreshableContainer, string name, int[,] diffusionWeights, int? denominator = null)
            : this(refreshableContainer,
                  $"{name} Diffusion",
                  new NamedBoundedValue("R", 4, (1, 32)),
                  new NamedBoundedValue("G", 4, (1, 32)),
                  new NamedBoundedValue("B", 4, (1, 32)),
                  diffusionWeights,
                  denominator) { }

        public ErrorDiffusionFilter(ErrorDiffusionFilter errorDiffusionFilter)
            : this(errorDiffusionFilter.RefreshableContainer,
                  errorDiffusionFilter.BaseName,
                  errorDiffusionFilter.RedSubdivisions.Parameters[0].Clone() as NamedBoundedValue,
                  errorDiffusionFilter.GreenSubdivisions.Parameters[0].Clone() as NamedBoundedValue,
                  errorDiffusionFilter.BlueSubdivisions.Parameters[0].Clone() as NamedBoundedValue,
                  errorDiffusionFilter.DiffusionWeightsKernel) {
        }

        public override object Clone()
            => new ErrorDiffusionFilter(this);
        #endregion
    }
}
