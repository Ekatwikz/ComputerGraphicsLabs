using System.Windows.Media;

namespace Task1Filters {
    public class GrayscaleFilter : Filter {
        protected sealed override byte[] ApplyFilterHook(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format) {
            for (int y = 0; y < bitmapPixelHeight; ++y) {
                for (int x = 0; x < bitmapPixelWidth; ++x) {
                    int index = y * backBufferStride + x * (format.BitsPerPixel / 8);

                    float average = 0;
                    for (int i = 0; i < 3; ++i) {
                        average += pixelBuffer[index + i] / 3.0F;
                    }

                    for (int i = 0; i < 3; ++i) {
                        pixelBuffer[index + i] = (byte)average;
                    }
                }
            }

            return pixelBuffer;
        }

        #region stuff
        public override string VerboseName => BaseName;
        #endregion

        #region creation
        public GrayscaleFilter(IRefreshableContainer refreshableContainer, string baseName = "Grayscale")
            : base(refreshableContainer) {
            BaseName = baseName;
        }

        public GrayscaleFilter(GrayscaleFilter grayscaleFilter)
            : this(grayscaleFilter.RefreshableContainer, grayscaleFilter.BaseName) { }

        public override object Clone()
            => new GrayscaleFilter(this);
        #endregion
    }
}
