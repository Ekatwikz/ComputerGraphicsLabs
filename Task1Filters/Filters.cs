using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Media;

namespace Task1Filters {
    public abstract class Filter : INotifyPropertyChanged, ICloneable {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) {
            cachedInputSHA1 = null; // cache invalid?
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _baseName;
        public string BaseName {
            get => _baseName;
            set {
                _baseName = value;
                OnPropertyChanged(nameof(BaseName));
                OnPropertyChanged(nameof(VerboseName));
            }
        }
        public abstract string VerboseName { get; } // TODO: just change this...

        // Need for Speed xdd
        private byte[] cachedInputSHA1, // TODO: CRC32 instead for moar speedz?
                        cachedOutputBuffer, cachedOutputSHA1;
        public (byte[], byte[]) ApplyFilter(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format, byte[] inputSHA1 = null) {
            byte[] outputBuffer;

            if (inputSHA1 == null || cachedInputSHA1 == null || !inputSHA1.SequenceEqual(cachedInputSHA1)) {
                Console.WriteLine($"[{VerboseName}] missed cache, recalculating");
                outputBuffer = ApplyFilterHook(pixelBuffer, bitmapPixelWidth, bitmapPixelHeight, backBufferStride, format);

                cachedOutputBuffer = new byte[outputBuffer.Length];
                outputBuffer.CopyTo(cachedOutputBuffer, 0);

                cachedInputSHA1 = new byte[inputSHA1.Length];
                inputSHA1.CopyTo(cachedInputSHA1, 0);

                using (SHA1 SHA1algorithm = SHA1.Create()) {
                    cachedOutputSHA1 = SHA1algorithm.ComputeHash(cachedOutputBuffer);
                }
            } else {
                Console.WriteLine($"[{VerboseName}] cache hit!");
                outputBuffer = new byte[cachedOutputBuffer.Length];
                cachedOutputBuffer.CopyTo(outputBuffer, 0);
            }

            byte[] outputSHA1 = new byte[cachedOutputSHA1.Length];
            cachedOutputSHA1.CopyTo(outputSHA1, 0);

            // fast for example when adding a filter to a huge image which already has a bunch of filters on it
            return (outputBuffer, outputSHA1);
        }

        protected abstract byte[] ApplyFilterHook(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format);

        public abstract object Clone(); // makes Presets easier?
    }

    public class ObservableFilterCollection : ObservableCollection<Filter> { }
}
