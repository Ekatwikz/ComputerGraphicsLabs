using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Task1Filters {
    public abstract class Filter : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged; // Unnecessary for some, but whatever
        protected void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public abstract string DisplayName { get; }

        public abstract byte[] applyFilter(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride);
    }

    public class ObservableFilterCollection : ObservableCollection<Filter> { }
}
