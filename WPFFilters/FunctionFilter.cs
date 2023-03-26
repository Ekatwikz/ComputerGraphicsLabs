using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace WPFFilters {
    public class FunctionFilter : Filter {
        protected sealed override byte[] ApplyFilterHook(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format) {
            for (int y = 0; y < bitmapPixelHeight; ++y) {
                for (int x = 0; x < bitmapPixelWidth; ++x) {
                    int index = y * backBufferStride + x * (format.BitsPerPixel / 8);

                    // 0: Blue, 1: Green, 2: Red?
                    for (int i = 0; i < 3; ++i) {
                        pixelBuffer[index + i] = Function.LookupTable[pixelBuffer[index + i]];
                    }
                }
            }

            return pixelBuffer;
        }

        #region stuff
        public override string VerboseName => string.Format("{0}{1}",
            BaseName,
            string.IsNullOrEmpty(Function.Info) ? "" : $" ({Function.Info})");

        public ByteFunctionDisplay Function { get; }
        #endregion

        #region creation
        protected FunctionFilter(IRefreshableContainer refreshableContainer, string baseName, ObservableCollection<NamedBoundedValue> parameters, Func<byte, Collection<NamedBoundedValue>, double> byteFunction)
            : base(refreshableContainer) {
            BaseName = baseName;
            Function = new ByteFunctionDisplay(this, parameters, byteFunction);
        }

        public FunctionFilter(IRefreshableContainer refreshableContainer, string baseName, Func<byte, Collection<NamedBoundedValue>, double> byteFunction, params NamedBoundedValue[] parameters)
            : this(refreshableContainer, baseName, new ObservableCollection<NamedBoundedValue>(parameters), byteFunction) { }

        public FunctionFilter(FunctionFilter functionFilter)
            : this(functionFilter.RefreshableContainer, functionFilter.BaseName, functionFilter.Function.Parameters, functionFilter.Function.ByteFunction) { }

        public override object Clone()
            => new FunctionFilter(this);
        #endregion
    }
}
