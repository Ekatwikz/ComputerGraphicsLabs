using Microsoft.Win32;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
        public string InputFileNameWithoutExtension => inputFileNameWithoutExtension_;

        public string InputFileName {
            get { return inputFileName_; }
            set {
                try {
                    OriginalBitmap = new WriteableBitmap(new BitmapImage(new Uri(value)));
                    inputFileNameWithoutExtension_ = Path.GetFileNameWithoutExtension(value);
                    inputFileName_ = value;
                } catch (Exception ex) {
                    MessageBox.Show(ex.Message, $"{ex.GetType()}: Couldn't load file", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private byte[] OriginalPixelBuffer { get; set; }
        private byte[] OriginalSHA1 { get; set; }

        private WriteableBitmap _originalBitmap;
        public WriteableBitmap OriginalBitmap {
            get => _originalBitmap;
            set {
                _originalBitmap = value;
                OriginalPixelBuffer = new byte[_originalBitmap.BackBufferStride * _originalBitmap.PixelHeight];

                _originalBitmap.CopyPixels(OriginalPixelBuffer, _originalBitmap.BackBufferStride, 0);
                using (SHA1 SHA1algorithm = SHA1.Create()) {
                    OriginalSHA1 = SHA1algorithm.ComputeHash(OriginalPixelBuffer);
                }

                FilteredBitmap = new WriteableBitmap(OriginalBitmap.PixelWidth, OriginalBitmap.PixelHeight, OriginalBitmap.DpiX, OriginalBitmap.DpiY, OriginalBitmap.Format, OriginalBitmap.Palette);
                OnPropertyChanged(nameof(OriginalBitmap));
                AutoRefreshFilters();
            }
        }

        private WriteableBitmap filteredBitmap_;
        public WriteableBitmap FilteredBitmap {
            get { return filteredBitmap_; }
            private set {
                filteredBitmap_ = value;
                OnPropertyChanged(nameof(FilteredBitmap));
            }
        }

        private const int INITIAL_IMAGE_WIDTH = 4;
        private const int INITIAL_IMAGE_HEIGHT = 3;

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

        private bool _autoRefresh = true;
        public bool AutoRefresh {
            get => _autoRefresh;
            set {
                _autoRefresh = value;
                AutoRefreshFilters();
                OnPropertyChanged(nameof(AutoRefresh));
            }
        }

        public Filter FilterToAdd { get; set; }
        public ObservableFilterCollection FilterMenuOptions { get; set; }
        #endregion

        public MainWindow() {
            InitializeComponent();

            #region filterPresets
            FilterCollection = new ObservableFilterCollection();
            FilterMenuOptions = new ObservableFilterCollection {
                // Pixe-by-pixel filters:
                new FunctionFilter("Inverse",
                    (inputByte, parameters) => (255 - inputByte).ClipToByte()
                ),

                new FunctionFilter(">0 Brightness",
                    (inputByte, parameters) => (inputByte + parameters[0].Value).ClipToByte(),
                    new NamedBoundedFilterParam("Correction",
                        20,
                        (0, 255)
                    )
                ),

                new FunctionFilter("<0 Brightness",
                    (inputByte, parameters) => (inputByte + parameters[0].Value).ClipToByte(),
                    new NamedBoundedFilterParam("Correction",
                        -20,
                        (-255, 0)
                    )
                ),

                 new FunctionFilter("Contrast",
                    (inputByte, parameters) => (parameters[0].Value * inputByte + 128 * (1 - parameters[0].Value)).ClipToByte(),
                    new NamedBoundedFilterParam("Enhancement",
                        1.5,
                        (1, 10)
                    )
                ),

                new FunctionFilter("<1 Gamma",
                    (inputByte, parameters) => (255D * Math.Pow(inputByte / 255D, parameters[0].Value)).ClipToByte(),
                    new NamedBoundedFilterParam("Level",
                        2D/3,
                        (0, 1)
                    )
                ),

                new FunctionFilter(">1 Gamma",
                    (inputByte, parameters) => (255D * Math.Pow(inputByte / 255D, parameters[0].Value)).ClipToByte(),
                    new NamedBoundedFilterParam("Level",
                        1.5,
                        (1, 10)
                    )
                ),

                new FunctionFilter("MultiParameterFilterDemo",
                    (inputByte, parameters) => ((parameters.Sum(param => param.Value) + inputByte) / 2).ClipToByte(),
                    new NamedBoundedFilterParam("A",
                        10,
                        (0, 64)
                    ),
                    new NamedBoundedFilterParam("B",
                        20,
                        (0, 64)
                    ),
                    new NamedBoundedFilterParam("C",
                        30,
                        (0, 64)
                    ),
                    new NamedBoundedFilterParam("D",
                        40,
                        (0, 63)
                    )
                ),

                // Convolution filters:
                new ConvolutionFilter("3x3 Box Blur", new int[,] {
                    {1, 1, 1},
                    {1, 1, 1},
                    {1, 1, 1},
                }),

                new ConvolutionFilter("9x9 Box Blur", new int[,] {
                    {1, 1, 1, 1, 1, 1, 1, 1, 1},
                    {1, 1, 1, 1, 1, 1, 1, 1, 1},
                    {1, 1, 1, 1, 1, 1, 1, 1, 1},
                    {1, 1, 1, 1, 1, 1, 1, 1, 1},
                    {1, 1, 1, 1, 1, 1, 1, 1, 1},
                    {1, 1, 1, 1, 1, 1, 1, 1, 1},
                    {1, 1, 1, 1, 1, 1, 1, 1, 1},
                    {1, 1, 1, 1, 1, 1, 1, 1, 1},
                    {1, 1, 1, 1, 1, 1, 1, 1, 1},
                }),

                new ConvolutionFilter("3x3 Gaussian Blur", new int[,] { // ??
                    {0, 1, 0},
                    {1, 4, 1},
                    {0, 1, 0},
                }),

                new ConvolutionFilter("5x5 Gaussian Blur", new int[,] { // ???
                    {0, 1, 2, 1, 0},
                    {1, 4, 8, 4, 1},
                    {2, 8, 16, 8, 2},
                    {1, 4, 8, 4, 1},
                    {0, 1, 2, 1, 0},
                }),

                new ConvolutionFilter("Sharpen", new int[,] {
                    {0, -1, 0},
                    {-1, 5, -1},
                    {0, -1, 0}
                }),

                new ConvolutionFilter("\"Mean Removal\" Sharpen", new int[,] {
                    {-1, -1, -1},
                    {-1, 9, -1},
                    {-1, -1, -1}
                }),

                new ConvolutionFilter("Horizontal Edge", new int[,] {
                    {0, -1, 0},
                    {0, 1, 0},
                    {0, 0, 0},
                }),

                new ConvolutionFilter("Vertical Edge", new int[,] {
                    {0, 0, 0},
                    {-1, 1, 0},
                    {0, 0, 0},
                }),

                new ConvolutionFilter("Diagonal Edge", new int[,] {
                    {-1, 0, 0},
                    {0, 1, 0},
                    {0, 0, 0},
                }),

                new ConvolutionFilter("Laplacian Edge A", new int[,] {
                    {0, -1, 0},
                    {-1, 4, -1},
                    {0, -1, 0},
                }),

                new ConvolutionFilter("Laplacian Edge B", new int[,] {
                    {-1, -1, -1},
                    {-1, 8, -1},
                    {-1, -1, -1},
                }),

                new ConvolutionFilter("East Emboss", new int[,] {
                    {-1, 0, 1},
                    {-1, 1, 1},
                    {-1, 0, 1},
                }),

                new ConvolutionFilter("South Emboss", new int[,] {
                    {-1, -1, -1},
                    {0, 1, 0},
                    {1, 1, 1},
                }),

                new ConvolutionFilter("South-East Emboss", new int[,] {
                    {-1, -1, 0},
                    {-1, 1, 1},
                    {0, 1, 1},
                })
            };
            #endregion
            DataContext = this;

            Bitmap blankWhiteBitmap = new Bitmap(INITIAL_IMAGE_WIDTH, INITIAL_IMAGE_HEIGHT);
            using (Graphics graphics = Graphics.FromImage(blankWhiteBitmap)) {
                graphics.Clear(System.Drawing.Color.White);
            }

            OriginalBitmap = new WriteableBitmap(blankWhiteBitmap.ToBitmapSource());
        }

        private void ApplyFilters() {
            FilteredBitmap.Lock();

            byte[] filteredPixelBuffer = new byte[OriginalPixelBuffer.Length],
                previousInputHash = OriginalSHA1;
            Array.Copy(OriginalPixelBuffer, filteredPixelBuffer, OriginalPixelBuffer.Length);

            foreach (Filter filter in FilterCollection) {
                (filteredPixelBuffer, previousInputHash) = filter.ApplyFilter(filteredPixelBuffer, OriginalBitmap.PixelWidth, OriginalBitmap.PixelHeight, FilteredBitmap.BackBufferStride, OriginalBitmap.Format, previousInputHash);
            }

            try {
                FilteredBitmap.WritePixels(new Int32Rect(0, 0, OriginalBitmap.PixelWidth, OriginalBitmap.PixelHeight), filteredPixelBuffer, FilteredBitmap.BackBufferStride, 0);
            } catch {
                throw new Exception("Error writing pixel buffer?");
            } finally {
                FilteredBitmap.Unlock();
            }
        }

        private void AutoRefreshFilters() {
            if (AutoRefresh || FilterCollection.Count == 0) {
                ApplyFilters();
            }
        }

        #region IOandStuff
        private void OpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Title = "Open",
                Filter = "Image Files (*.jpg *.jpeg *.png *.tiff)|*.jpg; *.jpeg; *.png; *.tiff|JPEG Files (*.jpg *.jpeg)|*.jpg; *.jpeg|PNG Files (*.png)|*.png|Tiff Files (*.tiff)|*.tiff"
            };

            if (openFileDialog.ShowDialog() == true) {
                InputFileName = openFileDialog.FileName;
            }
        }

        private void SaveAsCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            if (FilteredBitmap == null) {
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog {
                Title = "Save As ",
                Filter = "JPEG (*.jpg; *.jpeg)|*.jpg; *.jpeg",
                FileName = $"{InputFileNameWithoutExtension}_filtered"
            };

            if (saveFileDialog.ShowDialog() == true) {
                JpegBitmapEncoder jpegEncoder = new JpegBitmapEncoder();
                jpegEncoder.Frames.Add(BitmapFrame.Create(FilteredBitmap));
                using (FileStream fileStream = File.Create(saveFileDialog.FileName)) {
                    jpegEncoder.Save(fileStream);
                }
            }
        }

        private void OriginalImage_Drop(object sender, DragEventArgs e) {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];

            if (files != null && files.Length > 0) {
                InputFileName = files[0];
            } else if (e.Data.GetData(DataFormats.Bitmap) is WriteableBitmap writeableBitmap) {
                OriginalBitmap = writeableBitmap;
            } else {
                Console.WriteLine("What??");
            }
        }

        private void FilteredImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (sender is System.Windows.Controls.Image image) {
                DragDrop.DoDragDrop(image, new DataObject(DataFormats.Bitmap, image.Source), DragDropEffects.Copy);
            }
        }

        private void CopyCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            CopyImage(sender, e);
        }

        private void CopyImage(object sender, RoutedEventArgs e) {
            Clipboard.SetImage(FilteredBitmap.Clone());
        }

        private void PasteCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            PasteImage(sender, e);
        }

        private void PasteImage(object sender, RoutedEventArgs e) {
            StringCollection fileDropList = Clipboard.GetFileDropList();
            if (fileDropList.Count > 0) {
                InputFileName = fileDropList[0];
                return;
            }

            // TODO: ...?
            try {
                OriginalBitmap = new WriteableBitmap(Clipboard.GetDataObject().ToBitmapSource());
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
        #endregion

        #region controlsAndStuff
        private void ShowAboutBox(object sender, RoutedEventArgs e) {
            MessageBox.Show("Task1 - Filters\n" +
                "(Variant 2: With Convolution Filters' Editor)\n\n" +
                "Emmanuel Katwikirize",
                "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExitApp(object sender, RoutedEventArgs e) {
            Close();
        }

        private void ClearFiltersBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            FilterCollection.Clear();
            ApplyFilters();
        }

        private void DeleteFilter(object sender, RoutedEventArgs e) {
            object maybeFilter = (sender as Button).Tag;

            if (maybeFilter is Filter filter) {
                FilterCollection.Remove(filter);
            }

            AutoRefreshFilters();
        }

        private void RefreshCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            ApplyFilters();
        }

        private void AddFilter(object sender, RoutedEventArgs e) {
            if (FilterToAdd != null) {
                FilterCollection.Add((Filter)FilterToAdd.Clone());
                AutoRefreshFilters();
            }
        }

        private void ToggleAutoRefresh(object sender, RoutedEventArgs e) {
            AutoRefresh = !AutoRefresh;
        }

        private void SavePresetPrompt(object sender, RoutedEventArgs e) {
            object maybeFilter = (sender as Control).Tag;
            if (maybeFilter is Filter filter) {
                var window = new SavePresetWindow();
                var result = window.ShowDialog();
                string newPresetName = window.PresetNameBox.Text;

                if (result == true && newPresetName.Length > 0) {
                    Filter newPreset = (Filter)filter.Clone();

                    newPreset.BaseName = newPresetName;
                    filter.BaseName = newPresetName;
                    FilterMenuOptions.Add(newPreset);
                } else {
                    Console.WriteLine("Preset not saved?");
                }
            }
        }
        #endregion

        #region convolutionHacks
        private void ToggleConvolutionFilterDenominatorIsLinkedToKernel(object sender, RoutedEventArgs e) {
            object maybeConvolutionFilter = (sender as Button).Tag;

            if (maybeConvolutionFilter is ConvolutionFilter convolutionFilter) {
                convolutionFilter.ToggleDenominatorLink();
                AutoRefreshFilters();
            }
        }

        private void ToggleConvolutionFilterCenterPixelIsLinkedToKernel(object sender, RoutedEventArgs e) {
            object maybeConvolutionFilter = (sender as Button).Tag;

            if (maybeConvolutionFilter is ConvolutionFilter convolutionFilter) {
                convolutionFilter.ToggleCenterPixelLink();
                AutoRefreshFilters();
            }
        }

        private void NotifyConvolutionFilterKernelChanged(object sender, TextChangedEventArgs e) {
            object maybeConvolutionFilter = (sender as TextBox).Tag;

            if (maybeConvolutionFilter is ConvolutionFilter convolutionFilter) {
                convolutionFilter.NotifyKernelValuesChanged();
                AutoRefreshFilters();
            }
        }

        private void KernelChangedUpdateFilters(object sender, TextChangedEventArgs e) {
            AutoRefreshFilters();
        }

        private void ConvolutionFilterModifyKernelShape(object sender, RoutedEventArgs e) {
            object maybeConvolutionFilter = (sender as Button).Tag;

            if (maybeConvolutionFilter is ConvolutionFilter convolutionFilter) {
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

                convolutionFilter.ModifyKernelShape(kernelModificationFlags);
            }
        }

        private void ConvolutionFilterOffsetChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            object maybeConvolutionFilter = (sender as Slider).Tag;

            if (maybeConvolutionFilter is ConvolutionFilter filter) {
                filter.NotifyConvolutionOffsetChanged();
            }

            AutoRefreshFilters();
        }

        private void ConvolutionFilterResetOffset(object sender, RoutedEventArgs e) {
            object maybeConvolutionFilter = (sender as Button).Tag;

            if (maybeConvolutionFilter is ConvolutionFilter filter) {
                filter.Offset.Value = 0;
                filter.NotifyConvolutionOffsetChanged();
            }

            AutoRefreshFilters();
        }
        #endregion

        #region pixelHacks
        private void PixelFilterParamChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            object maybeConvolutionFilter = (sender as Slider).Tag;

            if (maybeConvolutionFilter is FunctionFilter filter) {
                filter.NotifyfilterParameterChanged();
            }

            AutoRefreshFilters();
        }
        #endregion
    }

    #region converters
    public class BooleanTemplateSelector : DataTemplateSelector {
        public DataTemplate TrueTemplate { get; set; }
        public DataTemplate FalseTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
            => item is bool boolValue && boolValue ? TrueTemplate : FalseTemplate;
    }

    [ValueConversion(typeof(bool), typeof(string))]
    internal class AutoRefreshBoolToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.Format("Toggle AutoRefresh: (Currently {0})",
                (bool)value ? "On" : "Off");

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    [ValueConversion(typeof(bool), typeof(SolidColorBrush))]
    public class AutoRefreshBooleanToColorConverter : IValueConverter {
        public SolidColorBrush TrueColor { get; set; }
        public SolidColorBrush FalseColor { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool boolean && boolean) ? TrueColor : FalseColor;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
    #endregion
}
