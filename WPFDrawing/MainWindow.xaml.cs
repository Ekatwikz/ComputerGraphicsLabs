using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPFDrawing {
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
                    DrawingBitmap = new WriteableBitmap(new BitmapImage(new Uri(value)));
                    _inputFileNameWithoutExtension = Path.GetFileNameWithoutExtension(value);
                    _inputFileName = value;
                } catch (Exception ex) {
                    ex.DisplayAsMessageBox("Couldn't load image from filename");
                }
            }
        }

        private byte[] DrawingPixelBuffer { get; set; }

        private WriteableBitmap _drawingBitmap;
        public WriteableBitmap DrawingBitmap {
            get => _drawingBitmap;
            set {
                _drawingBitmap = value;
                DrawingPixelBuffer = new byte[_drawingBitmap.BackBufferStride * _drawingBitmap.PixelHeight];

                _drawingBitmap.CopyPixels(DrawingPixelBuffer, _drawingBitmap.BackBufferStride, 0);

                OnPropertyChanged(nameof(DrawingBitmap));
                Refresh(true);
            }
        }

        public override (int, int) Bounds => (DrawingBitmap.PixelWidth, DrawingBitmap.PixelHeight);

        private const int INITIAL_IMAGE_WIDTH = 300;
        private const int INITIAL_IMAGE_HEIGHT = 300;

        private BaseBoundedCoord _selectedCoord;
        private BaseBoundedCoord SelectedCoord {
            get => _selectedCoord;
            set {
                _selectedCoord = value;
                OnPropertyChanged(nameof(SelectedCoord));
                Refresh();
            }
        }

        public DataContractSerializer ShapeSerializer { get; }

        private readonly Queue<Shape> _shapeSetupQueue;
        private void QueueShapeForSetup(Shape shape) {
            if (_shapeSetupQueue.Count == 0) {
                SelectedShape = shape;
                SelectedCoord = shape.GetNextSetupCoord(lastMouseDown: null);
            }

            _shapeSetupQueue.Enqueue(shape);
        }

        private void GetNextSetupCoordFromQueue(System.Windows.Point lastMouseDown) {
            // Lock in current setup point,
            // tell queued shape where we just clicked
            // ask it for next selected coord,
            // if that coord is null, dequeue the shape and get next

            // (assuming queue isn't already empty though!)
            Shape currentSetupShape = _shapeSetupQueue.Peek();
            SelectedCoord = currentSetupShape.GetNextSetupCoord(lastMouseDown);

            while (SelectedCoord == null && _shapeSetupQueue.Count > 0) {
                SelectedCoord = currentSetupShape.GetNextSetupCoord(lastMouseDown);
                if (SelectedCoord == null) {
                    _shapeSetupQueue.Dequeue();

                    if (_shapeSetupQueue.Count > 0) {
                        SelectedShape = currentSetupShape = _shapeSetupQueue.Peek();
                        SelectedCoord = currentSetupShape.GetNextSetupCoord(lastMouseDown);
                    }
                }
            }
        }

        private Shape _selectedShape;
        private Shape SelectedShape {
            get => _selectedShape;
            set {
                if (_selectedShape != null) {
                    _selectedShape.IsSelected = false;
                }

                _selectedShape = value;

                if (_selectedShape != null) {
                    _selectedShape.IsSelected = true;
                }

                OnPropertyChanged(nameof(SelectedShape));
                Refresh();
            }
        }

        private Shape _shapeToAdd;
        public Shape ShapeToAdd {
            get => _shapeToAdd;
            set {
                _shapeToAdd = value;
                OnPropertyChanged(nameof(ShapeToAdd));
            }
        }
        public ObservableCollection<Shape> ShapeMenuOptions { get; set; }

        private ObservableCollection<Shape> _shapeCollection; // TODO: change type/name
        public ObservableCollection<Shape> ShapeCollection {
            get {
                return _shapeCollection;
            }
            set {
                _shapeCollection = value;
                OnPropertyChanged(nameof(ShapeCollection));
            }
        }
        #endregion

        public MainWindow() {
            InitializeComponent();

            ShapeSerializer = new DataContractSerializer(typeof(Shape), new Type[] {
                typeof(VertexPoint)
            });

            ShapeCollection = new ObservableCollection<Shape>();
            _shapeSetupQueue = new Queue<Shape>();

            Bitmap blankWhiteBitmap = new Bitmap(INITIAL_IMAGE_WIDTH, INITIAL_IMAGE_HEIGHT);
            using (Graphics graphics = Graphics.FromImage(blankWhiteBitmap)) {
                graphics.Clear(System.Drawing.Color.White);
            }

            DrawingBitmap = new WriteableBitmap(blankWhiteBitmap.ToBitmapSource());

            // TMP
            VertexPoint tmp1 = new VertexPoint(this, null), tmp2 = new VertexPoint(this, null), tmp3 = new VertexPoint(this, null), tmp4 = tmp1;

            #region shapePresets
            ShapeMenuOptions = new ObservableCollection<Shape> {
                //// Task 3 shapes
                new VertexPoint(this, ShapeSerializer),
                new VertexPointController(this, ShapeSerializer, new VertexPoint(this, null), tmp2, tmp4),
                new VertexPointController(this, ShapeSerializer, new VertexPoint(this, null), tmp3),
                new Polygon(this, ShapeSerializer, new VertexPointController(this, ShapeSerializer, tmp1)) {
                    RenderSettingsProvider = this
                },
                new Circle(this, ShapeSerializer) {
                    RenderSettingsProvider = this
                },
                new Line(this, ShapeSerializer) {
                    RenderSettingsProvider = this
                }
            };
            #endregion
            DataContext = this;
        }

        protected override void RefreshHook() {
            DrawingBitmap.Lock();

            byte[] drawingBuffer = new byte[DrawingPixelBuffer.Length];
            Array.Copy(DrawingPixelBuffer, drawingBuffer, DrawingPixelBuffer.Length);

            foreach (Shape shape in ShapeCollection) {
                shape.DrawOnBitmapBuffer(drawingBuffer, DrawingBitmap.PixelWidth, DrawingBitmap.PixelHeight, DrawingBitmap.BackBufferStride, DrawingBitmap.Format);
            }

            try {
                DrawingBitmap.WritePixels(new Int32Rect(0, 0, DrawingBitmap.PixelWidth, DrawingBitmap.PixelHeight), drawingBuffer, DrawingBitmap.BackBufferStride, 0);
            } catch (Exception ex) {
                ex.DisplayAsMessageBox("??"); // ??
            } finally {
                DrawingBitmap.Unlock();
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
            if (DrawingBitmap == null) { // ????
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog {
                Title = "Save As",
                Filter = "JPEG (*.jpg; *.jpeg)|*.jpg; *.jpeg",
                FileName = $"{InputFileNameWithoutExtension}_edited"
            };

            if (saveFileDialog.ShowDialog() == true) {
                JpegBitmapEncoder jpegEncoder = new JpegBitmapEncoder();
                jpegEncoder.Frames.Add(BitmapFrame.Create(DrawingBitmap)); // ????
                using (FileStream fileStream = File.Create(saveFileDialog.FileName)) {
                    jpegEncoder.Save(fileStream);
                }
            }
        }

        private void OriginalImage_Drop(object sender, DragEventArgs e) {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0) {
                InputFileName = files[0];
            } else if (e.Data.GetData(DataFormats.Bitmap) is WriteableBitmap writeableBitmap) {
                DrawingBitmap = writeableBitmap;
            } else {
                Console.WriteLine("What??");
            }
        }

        private void CopyCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) { // TODO: remove me
            CopyImage(sender, e);
        }

        private void CopyImage(object sender, RoutedEventArgs e) {
            Clipboard.SetImage(DrawingBitmap.Clone()); // ????
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
                DrawingBitmap = new WriteableBitmap(Clipboard.GetDataObject().GetData(DataFormats.Bitmap) as BitmapSource);
            } catch (Exception ex) {
                ex.DisplayAsMessageBox("Couldn't get bitmap from clipboard");
            }
        }

        private void LoadShapeAsPreset(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Title = "Open",
                Filter = "Shape Presets (*.xml)|*.xml"
            };

            if (openFileDialog.ShowDialog() == true) {
                try {
                    using (FileStream stream = new FileStream(openFileDialog.FileName, FileMode.Open)) {
                        Shape loadedShape = (Shape)ShapeSerializer.ReadObject(stream);
                        loadedShape.RefreshableContainer = this;
                        loadedShape.RenderSettingsProvider = this;
                        ShapeMenuOptions.Add(loadedShape);
                    }

                    Console.WriteLine("Shape Loaded...");
                } catch (Exception ex) {
                    ex.DisplayAsMessageBox("Failed to load shape from preset");
                }
            }
        }
        #endregion

        #region controlsAndStuff
        private void ShowAboutBox(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("Task 3 - Rasterization\n" +
                "(Variant: Midpoint Line algorithms, Copying Pixels, Alternative Midpoint Circle, Xiaolin Wu)\n\n" +

                "Emmanuel Katwikirize\n" +
                "https://github.com/Ekatwikz",

                "About", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK) {
                System.Diagnostics.Process.Start("https://github.com/Ekatwikz");
            }
        }

        private void ExitApp(object sender, RoutedEventArgs e) {
            Close();
        }

        private void ClearShapesBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            ShapeCollection.Clear();
            _shapeSetupQueue.Clear();
            SelectedShape = null;
            SelectedCoord = null;
            Refresh(true);
        }

        private void DeleteShape(object sender, RoutedEventArgs e) {
            object maybeShape = (sender as Button).Tag;

            if (maybeShape is Shape shape) {
                ShapeCollection.Remove(shape);
            }

            Refresh(ShapeCollection.Count == 0);
        }

        private void RefreshCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            Refresh(true);
        }

        private void AddShape(object sender, RoutedEventArgs e) {
            if (ShapeToAdd != null) {
                Shape newShape = (Shape)ShapeToAdd.Clone();

                QueueShapeForSetup(newShape);
                ShapeCollection.Add(newShape);
                Refresh();
            }
        }
        #endregion

        #region mouseStuff
        private void DrawAreaImage_MouseMove(object sender, MouseEventArgs e) {
            if (SelectedCoord != null && (e.LeftButton == MouseButtonState.Pressed || _shapeSetupQueue.Count > 0)) {
                SelectedCoord.AsPoint = e.GetPosition(sender as System.Windows.Controls.Image);
            }
        }

        private void DrawAreaImage_MouseDown(object sender, MouseButtonEventArgs e) {
            System.Windows.Point clickPos = e.GetPosition(sender as System.Windows.Controls.Image);

            if (_shapeSetupQueue.Count > 0) {
                GetNextSetupCoordFromQueue(clickPos);
            } else if (e.LeftButton == MouseButtonState.Pressed) {
                double minDist = double.MaxValue;
                BaseBoundedCoord nearestCoord = null;
                Shape nearestShape = null;
                foreach (Shape shape in ShapeCollection) {
                    foreach (BaseBoundedCoord coord in shape.ClickableCoords) {
                        double dist = coord.AsPoint.DistanceFrom(clickPos);

                        if (dist <= minDist && clickPos.IsWithinRadiusOf(coord.AsPoint, shape.ClickableCoordRadius)) {
                            minDist = dist;
                            nearestShape = shape;
                            nearestCoord = coord;
                        }
                    }
                }

                SelectedCoord = nearestCoord;
                SelectedShape = nearestShape;
            }
        }

        private void DrawAreaImage_MouseUp(object sender, MouseEventArgs e) {
            if (_shapeSetupQueue.Count == 0) { // ?
                SelectedCoord = null;
                SelectedShape = null;
            }

            // else Nuffink... ?
        }

        private void ToggleAlias(object sender, RoutedEventArgs e) {
            CurrentRenderSettings ^= RenderSettings.XiaolinAlias;
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
