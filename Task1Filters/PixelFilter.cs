using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using static Task1Filters.PixelFilter;

namespace Task1Filters {
    public class PixelFilter : Filter {
        public sealed override byte[] applyFilter(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format) {
            OnPropertyChanged(VerboseName);
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

        private readonly byte[] lookupTable = new byte[256];
        public void recalculateLookupTable() {
            if (ByteFilterHook == null) { // ?
                return;
            }

            for (int i = 0; i < 256; ++i) {
                lookupTable[i] = ByteFilterHook((byte)i, Parameters);
            }
        }

        private ObservableCollection<PixelFilterParam> _parameters = new ObservableCollection<PixelFilterParam>();
        public ObservableCollection<PixelFilterParam> Parameters {
            get => _parameters;
            set {
                _parameters = new ObservableCollection<PixelFilterParam>();

                foreach (var param in value) {
                    _parameters.Add(param.Clone() as PixelFilterParam);
                }

                recalculateLookupTable();
            }
        }

        private Func<byte, Collection<PixelFilterParam>, byte> byteFilterHook_;
        protected Func<byte, Collection<PixelFilterParam>, byte> ByteFilterHook {
            get => byteFilterHook_;
            set {
                byteFilterHook_ = value;
                recalculateLookupTable();
            }
        }

        public class PixelFilterParam : INotifyPropertyChanged, ICloneable {
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName) {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public string BaseName { get; set; }
            public string VerboseName { get => $"{BaseName}: {Math.Round(Value, 3)}"; }

            public double LowerBound { get; set; }
            public double UpperBound { get; set; }

            private double value_;
            public double Value {
                get => value_;
                set {
                    if (LowerBound <= value && value <= UpperBound) {
                        value_ = value;
                        OnPropertyChanged(nameof(Value));
                        OnPropertyChanged(nameof(VerboseName));
                    }
                }
            }

            public PixelFilterParam(string baseName, double value, (double, double) bounds) {
                BaseName = baseName;
                LowerBound = bounds.Item1;
                UpperBound = bounds.Item2;
                Value = value;
            }

            public object Clone() {
                return MemberwiseClone();
            }
        }

        public void notifyfilterParameterChanged() {
            OnPropertyChanged(nameof(VerboseName));
            recalculateLookupTable();
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

        public PixelFilter(string name, Func<byte, Collection<PixelFilterParam>, byte> byteFilterHook, params PixelFilterParam[] parameters) {
            BaseName = name;
            Parameters = new ObservableCollection<PixelFilterParam>(parameters);
            ByteFilterHook = byteFilterHook;
        }

        public override object Clone() {
            PixelFilter clone = MemberwiseClone() as PixelFilter;
            clone.Parameters = Parameters;
            return clone;
        }
    }

    [ValueConversion(typeof(PixelFilterParam), typeof(double))]
    internal class PixelFilterParamTickFrequencyConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var param = (PixelFilterParam)value;
            return (param.UpperBound - param.LowerBound) / 10D;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
