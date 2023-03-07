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
        public void RecalculateLookupTable() {
            if (ByteFilterHook == null) { // ?
                return;
            }

            for (int i = 0; i < 256; ++i) {
                lookupTable[i] = ByteFilterHook((byte)i, Parameters).ClipToByte();
            }
        }

        private ObservableCollection<NamedBoundedFilterParam> _parameters = new ObservableCollection<NamedBoundedFilterParam>();
        public ObservableCollection<NamedBoundedFilterParam> Parameters {
            get => _parameters;
            set {
                _parameters = new ObservableCollection<NamedBoundedFilterParam>();

                foreach (var param in value) {
                    _parameters.Add(param.Clone() as NamedBoundedFilterParam);
                }

                RecalculateLookupTable();
            }
        }

        private Func<byte, Collection<NamedBoundedFilterParam>, double> byteFilterHook_;
        protected Func<byte, Collection<NamedBoundedFilterParam>, double> ByteFilterHook {
            get => byteFilterHook_;
            set {
                byteFilterHook_ = value;
                RecalculateLookupTable();
            }
        }

        public void NotifyfilterParameterChanged() {
            OnPropertyChanged(nameof(VerboseName));
            RecalculateLookupTable();
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

        public FunctionFilter(string name, Func<byte, Collection<NamedBoundedFilterParam>, double> byteFilterHook, params NamedBoundedFilterParam[] parameters) {
            BaseName = name;
            Parameters = new ObservableCollection<NamedBoundedFilterParam>(parameters);
            ByteFilterHook = byteFilterHook;
        }

        public override object Clone() {
            FunctionFilter clone = MemberwiseClone() as FunctionFilter;
            clone.lookupTable = new byte[256];
            clone.Parameters = Parameters;
            return clone;
        }
    }

    [ValueConversion(typeof(NamedBoundedFilterParam), typeof(double))]
    internal class PixelFilterParamTickFrequencyConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var param = (NamedBoundedFilterParam)value;
            return (param.UpperBound - param.LowerBound) / 10D;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
