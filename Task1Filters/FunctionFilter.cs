using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace Task1Filters {
    public class FunctionFilter : Filter {
        protected sealed override byte[] ApplyFilterHook(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format) {
            for (int y = 0; y < bitmapPixelHeight; ++y) {
                for (int x = 0; x < bitmapPixelWidth; ++x) {
                    int index = y * backBufferStride + x * (format.BitsPerPixel / 8);

                    // 0: Blue, 1: Green, 2: Red?
                    for (int i = 0; i < 3; ++i) {
                        pixelBuffer[index + i] = lookupTable[pixelBuffer[index + i]];
                    }
                }
            }

            return pixelBuffer;
        }

        private byte[] lookupTable = new byte[256];
        protected override void RefreshHook() {
            if (ByteFilterHook == null) { // ?
                return;
            }

            // recalculate lookup table of byte hook
            for (int i = 0; i < 256; ++i) {
                lookupTable[i] = ByteFilterHook((byte)i, Parameters).ClipToByte();
            }
        }

        private ObservableCollection<NamedBoundedValue> _parameters = new ObservableCollection<NamedBoundedValue>();
        public ObservableCollection<NamedBoundedValue> Parameters {
            get => _parameters;
            set {
                _parameters = new ObservableCollection<NamedBoundedValue>();

                foreach (var param in value) {
                    _parameters.Add(param.Clone() as NamedBoundedValue);
                }

                Refresh();
            }
        }

        private Func<byte, Collection<NamedBoundedValue>, double> _byteFilterHook;
        protected Func<byte, Collection<NamedBoundedValue>, double> ByteFilterHook {
            get => _byteFilterHook;
            set {
                _byteFilterHook = value;
                Refresh();
            }
        }

        public override string VerboseName {
            get {
                StringBuilder stringBuilder = new StringBuilder(BaseName);
                if (Parameters.Count > 0) {
                    stringBuilder.Append(" (");
                    for (int i = 0; i < Parameters.Count; ++i) {
                        stringBuilder.Append(Parameters[i].VerboseName);

                        if (i < Parameters.Count - 1) {
                            stringBuilder.Append(", ");
                        }
                    }
                    stringBuilder.Append(')');
                }

                return stringBuilder.ToString();
            }
        }

        public FunctionFilter(RefreshableWindow autorefreshableWindow, string name, Func<byte, Collection<NamedBoundedValue>, double> byteFilterHook, params NamedBoundedValue[] parameters)
            : base(autorefreshableWindow) {
            BaseName = name;
            Parameters = new ObservableCollection<NamedBoundedValue>(parameters);
            ByteFilterHook = byteFilterHook;

            // sus?
            foreach (NamedBoundedValue parameter in Parameters) {
                parameter.RefreshableContainer = this;
            }
        }

        public override object Clone() {
            FunctionFilter clone = MemberwiseClone() as FunctionFilter;
            clone.lookupTable = new byte[256];
            clone.Parameters = Parameters;
            foreach (NamedBoundedValue parameter in clone.Parameters) {
                parameter.RefreshableContainer = clone;
            }

            return clone;
        }
    }
}
