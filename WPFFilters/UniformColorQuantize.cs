using System.Collections.ObjectModel;
using System;
using System.Windows.Media;

namespace WPFFilters {
    public class UniformColorQuantize : Filter {
        protected override byte[] ApplyFilterHook(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format) {
            for (int y = 0; y < bitmapPixelHeight; ++y) {
                for (int x = 0; x < bitmapPixelWidth; ++x) {
                    int index = y * backBufferStride + x * (format.BitsPerPixel / 8);

                    pixelBuffer[index + 2] = RedFunctionDisplay.LookupTable[pixelBuffer[index + 2]];
                    pixelBuffer[index + 1] = GreenFunctionDisplay.LookupTable[pixelBuffer[index + 1]];
                    pixelBuffer[index] = BlueFunctionDisplay.LookupTable[pixelBuffer[index]];
                }
            }

            return pixelBuffer;
        }

        #region stuff
        public ByteFunctionDisplay RedFunctionDisplay { get; }
        public ByteFunctionDisplay GreenFunctionDisplay { get; }
        public ByteFunctionDisplay BlueFunctionDisplay { get; }

        public string Info
            => $"({RedFunctionDisplay.Info}, {GreenFunctionDisplay.Info}, {BlueFunctionDisplay.Info})"; // TODO: fix me

        public override string VerboseName
            => $"{BaseName}: {Info}";
        #endregion

        #region creation
        private UniformColorQuantize(IRefreshableContainer refreshableContainer, string baseName)
            : base(refreshableContainer) {
            BaseName = baseName;
        }

        public UniformColorQuantize(IRefreshableContainer refreshableContainer, string baseName, ByteFunctionDisplay redFunctionDisplay, ByteFunctionDisplay greenFunctionDisplay, ByteFunctionDisplay blueFunctionDisplay)
            : this(refreshableContainer, baseName) {
            RedFunctionDisplay = new ByteFunctionDisplay(this, redFunctionDisplay);
            GreenFunctionDisplay = new ByteFunctionDisplay(this, greenFunctionDisplay);
            BlueFunctionDisplay = new ByteFunctionDisplay(this, blueFunctionDisplay);
        }

        public UniformColorQuantize(IRefreshableContainer refreshableContainer, UniformColorQuantize uniformColorQuantize)
            : this(refreshableContainer,
                  uniformColorQuantize.BaseName,
                  uniformColorQuantize.RedFunctionDisplay,
                  uniformColorQuantize.GreenFunctionDisplay,
                  uniformColorQuantize.BlueFunctionDisplay) { }

        public UniformColorQuantize(UniformColorQuantize uniformColorQuantize)
            : this(uniformColorQuantize.RefreshableContainer, uniformColorQuantize) { }

        public UniformColorQuantize(IRefreshableContainer refreshableContainer, string baseName, Func<byte, Collection<NamedBoundedValue>, double> byteFunction, ObservableCollection<NamedBoundedValue> redParameters = null, ObservableCollection<NamedBoundedValue> greenParameters = null, ObservableCollection<NamedBoundedValue> blueParameters = null)
            : this(refreshableContainer, baseName) {
            RedFunctionDisplay = new ByteFunctionDisplay(this, redParameters, byteFunction);
            GreenFunctionDisplay = new ByteFunctionDisplay(this, greenParameters, byteFunction);
            BlueFunctionDisplay = new ByteFunctionDisplay(this, blueParameters, byteFunction);
        }

        public UniformColorQuantize(IRefreshableContainer refreshableContainer)
            : this(refreshableContainer,
                  "Uniform Color Quantize",
                  (inputByte, parameters) => {
                      int subdivisions = (int)Math.Round(parameters[0].Value);
                      return 256D / subdivisions * Math.Floor(inputByte * subdivisions / 256D) + 128D / subdivisions;
                  },
                  new ObservableCollection<NamedBoundedValue>() { new NamedBoundedValue("R", 4, (1, 32)) },
                  new ObservableCollection<NamedBoundedValue>() { new NamedBoundedValue("G", 4, (1, 32)) },
                  new ObservableCollection<NamedBoundedValue>() { new NamedBoundedValue("B", 4, (1, 32)) }) { }

        public override object Clone()
            => new UniformColorQuantize(this);
        #endregion
    }
}
