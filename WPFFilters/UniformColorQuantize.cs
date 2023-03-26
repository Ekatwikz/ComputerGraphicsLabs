using System.Collections.ObjectModel;
using System;
using System.Windows.Media;

namespace WPFFilters {
    public class UniformColorQuantize : Filter {
        protected sealed override byte[] ApplyFilterHook(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format) {
            for (int y = 0; y < bitmapPixelHeight; ++y) {
                for (int x = 0; x < bitmapPixelWidth; ++x) {
                    int index = y * backBufferStride + x * (format.BitsPerPixel / 8);

                    pixelBuffer[index + 2] = RedSubdivisions.LookupTable[pixelBuffer[index + 2]];
                    pixelBuffer[index + 1] = GreenSubdivisions.LookupTable[pixelBuffer[index + 1]];
                    pixelBuffer[index] = BlueSubdivisions.LookupTable[pixelBuffer[index]];
                }
            }

            return pixelBuffer;
        }

        #region stuff
        public ByteFunctionDisplay RedSubdivisions { get; }
        public ByteFunctionDisplay GreenSubdivisions { get; }
        public ByteFunctionDisplay BlueSubdivisions { get; }

        private Func<byte, Collection<NamedBoundedValue>, double> ByteFunction { get; }

        public override string VerboseName
            => $"{BaseName}: ({RedSubdivisions.Info}, {GreenSubdivisions.Info}, {BlueSubdivisions.Info})";
        #endregion

        #region creation
        public UniformColorQuantize(IRefreshableContainer refreshableContainer, string baseName, NamedBoundedValue redSubdivisionsParameter, NamedBoundedValue greenSubdivisionsParameter, NamedBoundedValue blueSubdivisionsParameter)
            : base(refreshableContainer) {
            BaseName = baseName;

            ByteFunction = (inputByte, parameters) => {
                int subdivisions = (int)Math.Round(parameters[0].Value);
                return 256D / subdivisions * Math.Floor(inputByte * subdivisions / 256D) + 128D / subdivisions;
            };

            RedSubdivisions = new ByteFunctionDisplay(this, ByteFunction, redSubdivisionsParameter);
            GreenSubdivisions = new ByteFunctionDisplay(this, ByteFunction, greenSubdivisionsParameter);
            BlueSubdivisions = new ByteFunctionDisplay(this, ByteFunction, blueSubdivisionsParameter);
        }

        public UniformColorQuantize(UniformColorQuantize uniformColorQuantize)
            : this(uniformColorQuantize.RefreshableContainer,
                  uniformColorQuantize.BaseName,
                  uniformColorQuantize.RedSubdivisions.Parameters[0].Clone() as NamedBoundedValue,
                  uniformColorQuantize.GreenSubdivisions.Parameters[0].Clone() as NamedBoundedValue,
                  uniformColorQuantize.BlueSubdivisions.Parameters[0].Clone() as NamedBoundedValue) { }

        public UniformColorQuantize(IRefreshableContainer refreshableContainer)
            : this(refreshableContainer,
                  "Uniform Color Quantize",
                  new NamedBoundedValue("R", 4, (1, 32)),
                  new NamedBoundedValue("G", 4, (1, 32)),
                  new NamedBoundedValue("B", 4, (1, 32))) { }

        public override object Clone()
            => new UniformColorQuantize(this);
        #endregion
    }
}
