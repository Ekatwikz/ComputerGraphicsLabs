using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace Task1Filters {
    public abstract class Filter : INotifyPropertyChanged, ICloneable {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) {
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

        public abstract byte[] applyFilter(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format);

        public abstract object Clone(); // makes Presets easier?
    }

    public class ObservableFilterCollection : ObservableCollection<Filter> { }
}
