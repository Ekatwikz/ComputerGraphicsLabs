using Microsoft.Win32;
using System;
using System.CodeDom;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Task1Filters {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged {
        #region properties
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string inputFileName_;
        private string inputFileNameWithoutExtension_;
        public string InputFileName {
            get { return inputFileName_; }
            set {
                OriginalBitmap = new BitmapImage(new Uri(value));
                inputFileNameWithoutExtension_ = System.IO.Path.GetFileNameWithoutExtension(value);
                inputFileName_ = value;
            }
        }

        public string InputFileNameWithoutExtension {
            get {
                return inputFileNameWithoutExtension_;
            }
        }

        private BitmapSource originalBitmap_;
        public BitmapSource OriginalBitmap {
            get { return originalBitmap_; }
            set {
                originalBitmap_ = value;
                OriginalImage.Source = value;
                OnPropertyChanged(nameof(OriginalBitmap));

                ApplyFilters();
            }
        }

        private BitmapSource filteredBitmap_;
        public BitmapSource FilteredBitmap {
            get { return filteredBitmap_; }
            set {
                filteredBitmap_ = value;
                FilteredImage.Source = value;
                OnPropertyChanged(nameof(FilteredBitmap));
            }
        }

        private const int DEFAULT_IMAGE_WIDTH = 100;
        private const int DEFAULT_IMAGE_HEIGHT = 100;
        #endregion

        public MainWindow() {
            InitializeComponent();

            Bitmap blankWhiteBitmap = new Bitmap(DEFAULT_IMAGE_WIDTH, DEFAULT_IMAGE_HEIGHT);
            using (Graphics graphics = Graphics.FromImage(blankWhiteBitmap)) {
                graphics.Clear(System.Drawing.Color.White);
            }

            OriginalBitmap = blankWhiteBitmap.toBitmapSource();
        }

        private void ApplyFilters() {
            if (OriginalBitmap == null) {
                return;
            }

            int width = OriginalBitmap.PixelWidth;
            int height = OriginalBitmap.PixelHeight;

            WriteableBitmap negativeBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            negativeBitmap.Lock();

            int stride = negativeBitmap.BackBufferStride;
            byte[] buffer = new byte[stride * height];
            OriginalBitmap.CopyPixels(buffer, stride, 0);

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int index = y * stride + 4 * x;
                    buffer[index + 0] = (byte)(255 - buffer[index + 0]); // blue
                    buffer[index + 1] = (byte)(255 - buffer[index + 1]); // green
                    buffer[index + 2] = (byte)(255 - buffer[index + 2]); // red
                }
            }

            try {
                negativeBitmap.WritePixels(new Int32Rect(0, 0, width, height), buffer, stride, 0);
            } finally {
                negativeBitmap.Unlock();
            }

            FilteredBitmap = negativeBitmap;
        }

        #region IOandStuff
        private void OpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select Image";
            openFileDialog.Filter = "JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg";

            if (openFileDialog.ShowDialog() == true) {
                InputFileName = openFileDialog.FileName;
            }
        }

        private void SaveAsCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            if (FilteredBitmap == null) {
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save Image as ";
            saveFileDialog.Filter = "JPEG (*.jpg; *.jpeg)|*.jpg; *.jpeg";
            saveFileDialog.FileName = $"{InputFileNameWithoutExtension}_filtered";

            if (saveFileDialog.ShowDialog() == true) {
                JpegBitmapEncoder jpegEncoder = new JpegBitmapEncoder();
                jpegEncoder.Frames.Add(BitmapFrame.Create(FilteredBitmap));
                using (FileStream fileStream = File.Create(saveFileDialog.FileName)) {
                    jpegEncoder.Save(fileStream);
                }
            }
        }

        private void OriginalImage_Drop(object sender, DragEventArgs e) {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files == null || files.Length == 0) { // Assume that the drop happened from the output image if there was no file associated?
                OriginalBitmap = ((WriteableBitmap)e.Data.GetData(DataFormats.Bitmap)).toBitmap().toBitmapSource();
            } else {
                InputFileName = files[0];
            }
        }

        private void FilteredImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (FilteredImage.Source != null) {
                DragDrop.DoDragDrop(FilteredImage, new DataObject(DataFormats.Bitmap, FilteredImage.Source), DragDropEffects.Copy);
            }
        }

        private void showAboutBox(object sender, RoutedEventArgs e) {
            MessageBox.Show("Task1 - Filters\n(Variant 2: With Convolution Filters' Editor)", "About", MessageBoxButton.OK);
        }

        private void exitApp(object sender, RoutedEventArgs e) {
            Close();
        }
        #endregion
    }
}
