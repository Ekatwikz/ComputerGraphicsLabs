using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

namespace WPFFilters {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RefreshableWindow {
        #region properties
        private string _inputFileName;
        private string _inputFileNameWithoutExtension;
        public string InputFileNameWithoutExtension => _inputFileNameWithoutExtension;

        public string InputFileName {
            get { return _inputFileName; }
            set {
                try {
                    OriginalBitmap = new WriteableBitmap(new BitmapImage(new Uri(value)));
                    _inputFileNameWithoutExtension = Path.GetFileNameWithoutExtension(value);
                    _inputFileName = value;
                } catch (Exception ex) {
                    ex.DisplayAsMessageBox("Couldn't load image from filename");
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
                Refresh(true);
            }
        }

        private WriteableBitmap _filteredBitmap;
        public WriteableBitmap FilteredBitmap {
            get { return _filteredBitmap; }
            private set {
                _filteredBitmap = value;
                OnPropertyChanged(nameof(FilteredBitmap));
            }
        }

        private const int INITIAL_IMAGE_WIDTH = 4;
        private const int INITIAL_IMAGE_HEIGHT = 3;

        private Filter _filterToAdd;
        public Filter FilterToAdd {
            get => _filterToAdd;
            set {
                _filterToAdd = value;
                OnPropertyChanged(nameof(FilterToAdd));
            }
        }
        public ObservableCollection<Filter> FilterMenuOptions { get; set; }

        private ObservableCollection<Filter> _filterCollection; // TODO: change type/name
        public ObservableCollection<Filter> FilterCollection {
            get {
                return _filterCollection;
            }
            set {
                _filterCollection = value;
                OnPropertyChanged(nameof(FilterCollection));
            }
        }
        #endregion

        public MainWindow() {
            InitializeComponent();

            FilterCollection = new ObservableCollection<Filter>();

            Bitmap blankWhiteBitmap = new Bitmap(INITIAL_IMAGE_WIDTH, INITIAL_IMAGE_HEIGHT);
            using (Graphics graphics = Graphics.FromImage(blankWhiteBitmap)) {
                graphics.Clear(System.Drawing.Color.White);
            }

            OriginalBitmap = new WriteableBitmap(blankWhiteBitmap.ToBitmapSource());

            #region filterPresets
            FilterMenuOptions = new ObservableCollection<Filter> {
                //// Task1 filters
                // Pixe-by-pixel filters:
                new FunctionFilter(this, "Inverse",
                    (inputByte, parameters) => 255 - inputByte
                ),

                new FunctionFilter(this, "Brightness",
                    (inputByte, parameters) => inputByte + parameters[0].Value,
                    new NamedBoundedValue("Correction",
                        20,
                        (-255, 255)
                    )
                ),

                new FunctionFilter(this, "Contrast",
                    (inputByte, parameters) => parameters[0].Value * inputByte + 128 * (1 - parameters[0].Value),
                    new NamedBoundedValue("Enhancement",
                        1.5,
                        (1, 10)
                    )
                ),

                new FunctionFilter(this, "<1 Gamma",
                    (inputByte, parameters) => 255 * Math.Pow(inputByte / 255D, parameters[0].Value),
                    new NamedBoundedValue("Level",
                        2D/3,
                        (0, 1)
                    )
                ),

                new FunctionFilter(this, ">1 Gamma",
                    (inputByte, parameters) => 255 * Math.Pow(inputByte / 255D, parameters[0].Value),
                    new NamedBoundedValue("Level",
                        1.5,
                        (1, 10)
                    )
                ),

                new FunctionFilter(this, "MultiParameterFilterDemo",
                    (inputByte, parameters) => (parameters.Sum(param => param.Value) + inputByte) / 2,
                    new NamedBoundedValue("A",
                        10,
                        (0, 64)
                    ),
                    new NamedBoundedValue("B",
                        20,
                        (0, 64)
                    ),
                    new NamedBoundedValue("C",
                        30,
                        (0, 64)
                    ),
                    new NamedBoundedValue("D",
                        40,
                        (0, 63)
                    )
                ),

                // Convolution filters:
                new ConvolutionFilter(this, "3x3 Box Blur", new int[,] {
                    {1, 1, 1},
                    {1, 1, 1},
                    {1, 1, 1},
                }),

                new ConvolutionFilter(this, "9x9 Box Blur", new int[,] {
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

                new ConvolutionFilter(this, "3x3 Gaussian Blur", new int[,] { // ??
                    {0, 1, 0},
                    {1, 4, 1},
                    {0, 1, 0},
                }),

                new ConvolutionFilter(this, "5x5 Gaussian Blur", new int[,] { // ???
                    {0, 1, 2, 1, 0},
                    {1, 4, 8, 4, 1},
                    {2, 8, 16, 8, 2},
                    {1, 4, 8, 4, 1},
                    {0, 1, 2, 1, 0},
                }),

                new ConvolutionFilter(this, "Sharpen", new int[,] {
                    {0, -1, 0},
                    {-1, 5, -1},
                    {0, -1, 0}
                }),

                new ConvolutionFilter(this, "\"Mean Removal\" Sharpen", new int[,] {
                    {-1, -1, -1},
                    {-1, 9, -1},
                    {-1, -1, -1}
                }),

                new ConvolutionFilter(this, "Horizontal Edge", new int[,] {
                    {0, -1, 0},
                    {0, 1, 0},
                    {0, 0, 0},
                }),

                new ConvolutionFilter(this, "Vertical Edge", new int[,] {
                    {0, 0, 0},
                    {-1, 1, 0},
                    {0, 0, 0},
                }),

                new ConvolutionFilter(this, "Diagonal Edge", new int[,] {
                    {-1, 0, 0},
                    {0, 1, 0},
                    {0, 0, 0},
                }),

                new ConvolutionFilter(this, "Laplacian Edge A", new int[,] {
                    {0, -1, 0},
                    {-1, 4, -1},
                    {0, -1, 0},
                }),

                new ConvolutionFilter(this, "Laplacian Edge B", new int[,] {
                    {-1, -1, -1},
                    {-1, 8, -1},
                    {-1, -1, -1},
                }),

                new ConvolutionFilter(this, "East Emboss", new int[,] {
                    {-1, 0, 1},
                    {-1, 1, 1},
                    {-1, 0, 1},
                }),

                new ConvolutionFilter(this, "South Emboss", new int[,] {
                    {-1, -1, -1},
                    {0, 1, 0},
                    {1, 1, 1},
                }),

                new ConvolutionFilter(this, "South-East Emboss", new int[,] {
                    {-1, -1, 0},
                    {-1, 1, 1},
                    {0, 1, 1},
                }),

                // Dual Kernel crap from Task1 Lab part
                new DualKernelConvolutionFilter(this, new int[,] {
                    {0, -1, 0},
                    {0, 1, 0},
                    {0, 0, 0},
                }, new int[,] {
                    {0, 0, 0},
                    {-1, 1, 0},
                    {0, 0, 0},
                }),

                //// Task2 filters
                new GrayscaleFilter(this),
                new UniformColorQuantize(this),

                new ErrorDiffusionFilter(this, "Floyd-Steinberg", new int[,] {
                    {0, 0, 0},
                    {0, 0, 7},
                    {3, 5, 1},
                }),

                new ErrorDiffusionFilter(this, "Floyd-Steinberg 3Bit",
                    new UniformColorQuantize(null, null, (inputByte, parameters) => inputByte > 127 ? 255 : 0),
                    new int[,] {
                    {0, 0, 0},
                    {0, 0, 7},
                    {3, 5, 1},
                }),

                new ErrorDiffusionFilter(this, "Burkes", new int[,] {
                    {0, 0, 0, 0, 0},
                    {0, 0, 0, 8, 4},
                    {2, 4, 8, 4, 2},
                }),

                new ErrorDiffusionFilter(this, "Stucky", new int[,] {
                    {0, 0, 0, 0, 0},
                    {0, 0, 0, 0, 0},
                    {0, 0, 0, 8, 4},
                    {2, 4, 8, 4, 2},
                    {1, 2, 4, 2, 1},
                }),

                new ErrorDiffusionFilter(this, "Sierra", new int[,] {
                    {0, 0, 0, 0, 0},
                    {0, 0, 0, 0, 0},
                    {0, 0, 0, 5, 3},
                    {2, 4, 5, 4, 2},
                    {0, 2, 3, 2, 0},
                }),

                new ErrorDiffusionFilter(this, "Atkinson", new int[,] {
                    {0, 0, 0, 0, 0},
                    {0, 0, 0, 0, 0},
                    {0, 0, 0, 1, 1},
                    {0, 1, 1, 1, 0},
                    {0, 0, 1, 0, 0},
                }, denominator: 8),
            };
            #endregion
            DataContext = this;
        }

        protected override void RefreshHook() {
            FilteredBitmap.Lock();

            byte[] filteredPixelBuffer = new byte[OriginalPixelBuffer.Length],
                previousInputHash = OriginalSHA1;
            Array.Copy(OriginalPixelBuffer, filteredPixelBuffer, OriginalPixelBuffer.Length);

            foreach (Filter filter in FilterCollection) {
                (filteredPixelBuffer, previousInputHash) = filter.ApplyFilter(filteredPixelBuffer, OriginalBitmap.PixelWidth, OriginalBitmap.PixelHeight, FilteredBitmap.BackBufferStride, OriginalBitmap.Format, previousInputHash);
            }

            try {
                FilteredBitmap.WritePixels(new Int32Rect(0, 0, OriginalBitmap.PixelWidth, OriginalBitmap.PixelHeight), filteredPixelBuffer, FilteredBitmap.BackBufferStride, 0);
            } catch (Exception ex) {
                ex.DisplayAsMessageBox("??"); // ??
            } finally {
                FilteredBitmap.Unlock();
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
                Title = "Save As",
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
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0) {
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

        private void CopyCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) { // TODO: remove me
            CopyImage(sender, e);
        }

        private void CopyImage(object sender, RoutedEventArgs e) {
            Clipboard.SetImage(FilteredBitmap.Clone());
        }

        private void PasteCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) { // TODO: remove me
            PasteImage(sender, e);
        }

        private void PasteImage(object sender, RoutedEventArgs e) {
            StringCollection fileDropList = Clipboard.GetFileDropList();
            if (fileDropList.Count > 0) {
                InputFileName = fileDropList[0];
                return;
            }

            try {
                OriginalBitmap = new WriteableBitmap(Clipboard.GetDataObject().GetData(DataFormats.Bitmap) as BitmapSource);
            } catch (Exception ex) {
                ex.DisplayAsMessageBox("Couldn't get bitmap from clipboard");
            }
        }
        #endregion

        #region controlsAndStuff
        private void ShowAboutBox(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Task1 - Filters\n" +
                "(Variant 2: + Convolution Filters' Editor)\n\n" +
                "Task 2 - Dithering/Quantization\n" +
                "(Variant: Error Diffusion and Uniform Quantization)\n\n" +

                "Emmanuel Katwikirize\n" +
                "https://github.com/Ekatwikz",

                "About", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK) {
                System.Diagnostics.Process.Start("https://github.com/Ekatwikz");
            }
        }

        private void ExitApp(object sender, RoutedEventArgs e) {
            Close();
        }

        private void ClearFiltersBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            FilterCollection.Clear();
            Refresh(true);
        }

        private void DeleteFilter(object sender, RoutedEventArgs e) {
            object maybeFilter = (sender as Button).Tag;

            if (maybeFilter is Filter filter) {
                FilterCollection.Remove(filter);
            }

            Refresh(FilterCollection.Count == 0);
        }

        private void RefreshCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            Refresh(true);
        }

        private void AddFilter(object sender, RoutedEventArgs e) {
            if (FilterToAdd != null) {
                FilterCollection.Add((Filter)FilterToAdd.Clone());
                Refresh();
            }
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

                    FilterToAdd = newPreset;
                } else {
                    Console.WriteLine("Preset not saved?");
                }
            }
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

    [ValueConversion(typeof(bool), typeof(SolidColorBrush))]
    public class BooleanToColorConverter : IValueConverter {
        public SolidColorBrush TrueColor { get; set; }
        public SolidColorBrush FalseColor { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool boolean && boolean) ? TrueColor : FalseColor;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    [ValueConversion(typeof(bool), typeof(string))]
    internal class AutoRefreshBoolToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.Format("Toggle AutoRefresh: (Currently {0})",
                (bool)value ? "On" : "Off");

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    #endregion
}
