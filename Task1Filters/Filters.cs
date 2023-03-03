using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace Task1Filters {
    public abstract class Filter : INotifyPropertyChanged, ICloneable {
        public event PropertyChangedEventHandler PropertyChanged; // Unnecessary for some, but whatever
        protected void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string BaseName { get; set; }
        public abstract string VerboseName { get; }

        public abstract byte[] applyFilter(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format);

        public abstract object Clone(); // makes Presets easier?
    }

    public class ObservableFilterCollection : ObservableCollection<Filter> { }
}
