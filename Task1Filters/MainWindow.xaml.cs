using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
                try {
                    OriginalBitmap = new BitmapImage(new Uri(value));
                    inputFileNameWithoutExtension_ = System.IO.Path.GetFileNameWithoutExtension(value);
                    inputFileName_ = value;
                } catch (Exception ex) {
                    MessageBox.Show(ex.Message, $"Couldn't load file: {ex.GetType()}", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        public string InputFileNameWithoutExtension {
            get {
                return inputFileNameWithoutExtension_;
            }
        }

        // TODO: Keep Bitmap too?
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

        private const int INITIAL_IMAGE_WIDTH = 20;
        private const int INITIAL_IMAGE_HEIGHT = 20;

        private ObservableFilterCollection filterCollection_;
        public ObservableFilterCollection FilterCollection {
            get {
                return filterCollection_;
            }
            set {
                filterCollection_ = value;
                OnPropertyChanged(nameof(FilterCollection));
            }
        }

        public Filter FilterToAdd { get; set; }
        public List<Filter> FilterMenuOptions {
            get => new List<Filter> {
                new InverseFilter()
                , new GammaCorrectionFilter{ GammaLevel = 1.5 }
                , new ConvolutionFilter("Box Blur", new int[,] {
                    {1, 1, 1},
                    {1, 1, 1},
                    {1, 1, 1}
                })
            };
        }

        private bool filterIsRunning_;
        public bool FilterIsRunning { // TODO: use this in UI?
            get => filterIsRunning_;
            set {
                value = filterIsRunning_;
                OnPropertyChanged(nameof(FilterIsRunning));
            }
        }
        #endregion

        public MainWindow() {
            InitializeComponent();

            FilterCollection = new ObservableFilterCollection();
            FilterIsRunning = false;

            DataContext = this;

            Bitmap blankWhiteBitmap = new Bitmap(INITIAL_IMAGE_WIDTH, INITIAL_IMAGE_HEIGHT);
            using (Graphics graphics = Graphics.FromImage(blankWhiteBitmap)) {
                graphics.Clear(System.Drawing.Color.White);
            }

            OriginalBitmap = blankWhiteBitmap.toBitmapSource();
        }

        private void ApplyFilters() {
            if (OriginalBitmap == null) {
                return;
            }

            WriteableBitmap filteredBitmap = new WriteableBitmap(OriginalBitmap.PixelWidth, OriginalBitmap.PixelHeight, OriginalBitmap.DpiX, OriginalBitmap.DpiY, OriginalBitmap.Format, OriginalBitmap.Palette);
            filteredBitmap.Lock();

            byte[] pixelBuffer = new byte[filteredBitmap.BackBufferStride * OriginalBitmap.PixelHeight];
            OriginalBitmap.CopyPixels(pixelBuffer, filteredBitmap.BackBufferStride, 0);

            FilterIsRunning = true;
            foreach (Filter filter in FilterCollection) {
                pixelBuffer = filter.applyFilter(pixelBuffer, OriginalBitmap.PixelWidth, OriginalBitmap.PixelHeight, filteredBitmap.BackBufferStride, OriginalBitmap.Format);
            }
            FilterIsRunning = false;

            try {
                filteredBitmap.WritePixels(new Int32Rect(0, 0, OriginalBitmap.PixelWidth, OriginalBitmap.PixelHeight), pixelBuffer, filteredBitmap.BackBufferStride, 0);
            } finally {
                filteredBitmap.Unlock();
            }

            FilteredBitmap = filteredBitmap;
        }

        #region IOandStuff
        private void OpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Open";
            openFileDialog.Filter = "Image Files (*.jpg *.jpeg *.png *.tiff)|*.jpg; *.jpeg; *.png; *.tiff|JPEG Files (*.jpg *.jpeg)|*.jpg; *.jpeg|PNG Files (*.png)|*.png|Tiff Files (*.tiff)|*.tiff";

            if (openFileDialog.ShowDialog() == true) {
                InputFileName = openFileDialog.FileName;
            }
        }

        private void SaveAsCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            if (FilteredBitmap == null) {
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save As ";
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
                OriginalBitmap = e.Data.toBitmapSource();
            } else {
                InputFileName = files[0];
            }
        }

        private void FilteredImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (FilteredImage.Source != null) {
                DragDrop.DoDragDrop(FilteredImage, new DataObject(DataFormats.Bitmap, FilteredImage.Source), DragDropEffects.Copy);
            }
        }

        private void CopyCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            copyImage(sender, e);
        }

        private void copyImage(object sender, RoutedEventArgs e) {
            Clipboard.SetImage(FilteredBitmap.Clone());
        }

        private void PasteCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            pasteImage(sender, e);
        }

        private void pasteImage(object sender, RoutedEventArgs e) {
            StringCollection fileDropList = Clipboard.GetFileDropList();
            if (fileDropList.Count > 0) {
                InputFileName = fileDropList[0];
                return;
            }

            // TODO: ...?
            BitmapSource image = Clipboard.GetDataObject().toBitmapSource();
            if (image != null) {
                OriginalBitmap = image;
            }
        }
        #endregion

        #region controlsAndStuff
        private void showAboutBox(object sender, RoutedEventArgs e) {
            MessageBox.Show("Task1 - Filters\n" +
                "(Variant 2: With Convolution Filters' Editor)\n" +
                "Emmanuel Katwikirize",
                "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void exitApp(object sender, RoutedEventArgs e) {
            Close();
        }

        private void ClearFiltersBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            FilterCollection.Clear();
            ApplyFilters();
        }

        private void deleteFilter(object sender, RoutedEventArgs e) {
            object maybeFilter = (sender as Button).Tag;

            if (maybeFilter is Filter) {
                FilterCollection.Remove(maybeFilter as Filter);
            }

            ApplyFilters();
        }

        private void RefreshCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            ApplyFilters();
        }

        private void addFilter(object sender, RoutedEventArgs e) {
            if (FilterToAdd != null) {
                FilterCollection.Add((Filter)FilterToAdd.Clone());
                ApplyFilters();
            }
        }
        #endregion

        #region convolutionHacks
        private void toggleConvolutionFilterDenominatorIsLinkedToKernel(object sender, RoutedEventArgs e) {
            object maybeConvolutionFilter = (sender as Button).Tag;

            if (maybeConvolutionFilter is ConvolutionFilter) {
                (maybeConvolutionFilter as ConvolutionFilter).toggleKernelLink();
            }
        }

        private void notifyConvolutionFilterKernelChanged(object sender, TextChangedEventArgs e) {
            object maybeConvolutionFilter = (sender as TextBox).Tag;

            if (maybeConvolutionFilter is ConvolutionFilter) {
                (maybeConvolutionFilter as ConvolutionFilter).notifyKernelValuesChanged();
            }
        }

        private void convolutionFilterModifyKernelShape(object sender, RoutedEventArgs e) {
            object maybeConvolutionFilter = (sender as Button).Tag;

            if (maybeConvolutionFilter is ConvolutionFilter) {
                ConvolutionFilter.KernelModificationFlags kernelModificationFlags = 0x0;

                string[] buttonNameParts = (sender as Button).Name.Split('_');
                string buttonAction = buttonNameParts[0];
                string buttonActionLocation = buttonNameParts[1];

                switch (buttonAction) {
                    case "add":
                        kernelModificationFlags |= ConvolutionFilter.KernelModificationFlags.SHOULDADD;
                        break;
                    case "remove":
                        break;
                    default:
                        throw new ArgumentException("Bad button action");
                }

                switch (buttonActionLocation) {
                    case "top":
                        kernelModificationFlags |= ConvolutionFilter.KernelModificationFlags.TOP;
                        break;
                    case "bottom":
                        kernelModificationFlags |= ConvolutionFilter.KernelModificationFlags.BOTTOM;
                        break;
                    case "left":
                        kernelModificationFlags |= ConvolutionFilter.KernelModificationFlags.LEFT;
                        break;
                    case "right":
                        kernelModificationFlags |= ConvolutionFilter.KernelModificationFlags.RIGHT;
                        break;
                    default:
                        throw new ArgumentException("Bad button action location");
                }

                (maybeConvolutionFilter as ConvolutionFilter).modifyKernelShape(kernelModificationFlags);
            }
        }
        #endregion
    }
}
