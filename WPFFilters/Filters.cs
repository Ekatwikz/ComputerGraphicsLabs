using System;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Media;

namespace WPFFilters {
    // TODO: add interface?
    public abstract class Filter : NamedMemberOfRefreshable, ICloneable, IRefreshableContainer {
        // Need for Speed xdd
        private byte[] _cachedInputSHA1, // TODO: CRC32 instead for moar speedz?
                        _cachedOutputBuffer, _cachedOutputSHA1;
        public (byte[], byte[]) ApplyFilter(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format, byte[] inputSHA1) {
            byte[] outputBuffer;

            if (inputSHA1 == null || _cachedInputSHA1 == null || !inputSHA1.SequenceEqual(_cachedInputSHA1)) {
                Console.WriteLine($"[{VerboseName}] missed cache, recalculating");
                outputBuffer = ApplyFilterHook(pixelBuffer, bitmapPixelWidth, bitmapPixelHeight, backBufferStride, format);

                _cachedOutputBuffer = new byte[outputBuffer.Length];
                outputBuffer.CopyTo(_cachedOutputBuffer, 0);

                _cachedInputSHA1 = new byte[inputSHA1.Length];
                inputSHA1.CopyTo(_cachedInputSHA1, 0);

                using (SHA1 SHA1algorithm = SHA1.Create()) {
                    _cachedOutputSHA1 = SHA1algorithm.ComputeHash(_cachedOutputBuffer);
                }
            } else {
                Console.WriteLine($"[{VerboseName}] cache hit!");
                outputBuffer = new byte[_cachedOutputBuffer.Length];
                _cachedOutputBuffer.CopyTo(outputBuffer, 0);
            }

            byte[] outputSHA1 = new byte[_cachedOutputSHA1.Length];
            _cachedOutputSHA1.CopyTo(outputSHA1, 0);

            // fast for example when adding a filter to a huge image which already has a bunch of filters on it
            return (outputBuffer, outputSHA1);
        }

        protected abstract byte[] ApplyFilterHook(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format);

        public Filter(IRefreshableContainer refreshableContainer) {
            RefreshableContainer = refreshableContainer;
        }

        public void Refresh(bool forceRefresh = true) { // param ignored?
            OnPropertyChanged(nameof(VerboseName));
            _cachedInputSHA1 = null; // force cache miss on next apply call?
            RefreshableContainer.Refresh();
        }

        public abstract string VerboseName { get; } // TODO: just change this...
        protected override void BaseNameChangedHook() => OnPropertyChanged(nameof(VerboseName));

        // TODO: implement base part of this once parameters are moved here?
        public abstract object Clone(); // makes Presets easier?
    }
}
