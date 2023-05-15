using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPFDrawing {
    [DataContract]
    [KnownType(typeof(Rectangle))]
    public class Polygon : Shape {
        public override RGBCoord[] PixelCoords {
            get {
                List<RGBCoord> pixels = new List<RGBCoord>(9);

                if (IsFilled) {
                    Fill(pixels);
                }

                foreach (RGBCoord coord in Middle.PixelCoords) {
                    pixels.Add(coord);
                }

                DrawLines(pixels);

                foreach (Line line in ClipHighlighterLines()) {
                    foreach (RGBCoord coord in line.PixelCoords) {
                        pixels.Add(coord.Invert());
                    }
                }

                return pixels.ToArray();
            }
        }

        #region clipAndFill
        public List<Line> ClipHighlighterLines() {
            List<Line> lines = new List<Line>();

            if (ClippingRect == null) {
                return lines;
            }

            foreach (Line line in DrawLines()) {
                lines.Add(line);
            }

            return lines;
        }

        public void Fill(List<RGBCoord> pixels) {
            Console.WriteLine("FILLED!");

            pixels.Add(new RGBCoord {
                Coord = (1, 1),
                CoordColor = FillColor.SelectedColor
            });

            if (FillImage != null) {
                Console.WriteLine("IMAGE FILLED!");
                pixels.Add(new RGBCoord {
                    Coord = (5, 5),
                    CoordColor = FillColor.SelectedColor
                });
            }
        }
        #endregion

        #region stuff
        private VertexPointController _middle;
        [DataMember]
        public VertexPointController Middle {
            get => _middle;
            protected set {
                _middle = value;
                _middle.RefreshableContainer = this;
            }
        }

        public HashSet<BoundedCoord> Vertices => Middle.CoordController.ControlledCoords;

        public virtual List<Line> DrawLines(List<RGBCoord> pixels = null) {
            List<Line> lines = new List<Line>();

            BoundedCoord prevCoord = null;
            foreach (BoundedCoord vertex in Vertices) {
                if (prevCoord != null) {
                    Line line = new Line(this, null, Color, MoveDirection.BOTH, new VertexPoint(prevCoord, false), new VertexPoint(vertex, false)) {
                        RenderSettingsProvider = RenderSettingsProvider,
                        Thickness = Thickness
                    };
                    lines.Add(line);
                }

                prevCoord = vertex;
            }

            if (Vertices.Count > 1) {
                Line lastLine = new Line(this, null, Color, MoveDirection.BOTH, new VertexPoint(prevCoord, false), new VertexPoint(Vertices.First(), false)) {
                    RenderSettingsProvider = RenderSettingsProvider,
                    Thickness = Thickness
                };
                lines.Add(lastLine);
            }

            if (pixels != null) {
                foreach (Line line in lines) {
                    foreach (RGBCoord coord in line.PixelCoords) {
                        pixels.Add(coord);
                    }
                }
            }

            return lines;
        }

        // lame idea, but it's not the first around here lol
        public ((double, double), (double, double)) BoundingBox() {
            double xMin = double.MaxValue,
                xMax = double.MinValue,
                yMin = double.MaxValue,
                yMax = double.MinValue;

            foreach (Line line in DrawLines()) {
                foreach (BoundedCoord coord in line.Middle.CoordController.ControlledCoords) {
                    if (coord.AsPoint.X < xMin) {
                        xMin = coord.AsPoint.X;
                    }

                    if (coord.AsPoint.X > xMax) {
                        xMax = coord.AsPoint.X;
                    }

                    if (coord.AsPoint.Y < yMin) {
                        yMin = coord.AsPoint.Y;
                    }

                    if (coord.AsPoint.Y > yMax) {
                        yMax = coord.AsPoint.Y;
                    }
                }
            }

            // ugh:
            if (xMin == double.MaxValue || xMax == double.MinValue || yMin == double.MaxValue || yMax == double.MinValue) {
                xMin = 0;
                xMax = 0;
                yMin = 0;
                yMax = 0;
            }

            return ((xMin, xMax), (yMin, yMax));
        }

        public Rectangle ClippingRect { get; set; }

        private bool _isFilled;
        [DataMember]
        public bool IsFilled {
            get => _isFilled;
            set {
                _isFilled = value; // TODO: COPY??
                OnPropertyChanged(nameof(IsFilled));
                RefreshableContainer?.Refresh();
            }
        }

        private byte[] _fillImageBytes;
        [DataMember]
        public byte[] FillImageBytes {
            get => _fillImageBytes;
            set {
                _fillImageBytes = value;
                if (value != null) {
                    _fillImage = _fillImageBytes.DeserializeToWriteableBitmap();
                }

                OnPropertyChanged(nameof(FillImageBytes));
                RefreshableContainer?.Refresh();
            }
        }

        private WriteableBitmap _fillImage;
        protected WriteableBitmap FillImage {
            get { // return a stretched bitmap without modifying the original? idk
                if (_fillImage == null) {
                    return null;
                }

                ((double, double), (double, double)) boundingBox = BoundingBox();
                int newWidth = (int)(boundingBox.Item1.Item2 - boundingBox.Item1.Item1); // Your new width
                int newHeight = (int)(boundingBox.Item2.Item2 - boundingBox.Item2.Item1); ; // Your new height
                if (newWidth == 0 || newHeight == 0) {
                    return null; // (usually?) when rectangle is in setup mode
                }

                // Create a new WriteableBitmap with the new dimensions
                //WriteableBitmap stretchedBitmap = new WriteableBitmap(newWidth, newHeight, _fillImage.DpiX, _fillImage.DpiY, PixelFormats.Default, null);
                WriteableBitmap stretchedBitmap = new WriteableBitmap(newWidth, newHeight, _fillImage.DpiX, _fillImage.DpiY, _fillImage.Format, null);

                // Use a DrawingVisual and RenderTargetBitmap to render the original bitmap onto the new one, scaled to fit
                DrawingVisual drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen()) {
                    drawingContext.DrawImage(_fillImage, new Rect(0, 0, newWidth, newHeight));
                }
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(newWidth, newHeight, _fillImage.DpiX, _fillImage.DpiY, PixelFormats.Default);
                renderTargetBitmap.Render(drawingVisual);

                // Copy the rendered image to the new WriteableBitmap
                stretchedBitmap.Lock();
                renderTargetBitmap.CopyPixels(new Int32Rect(0, 0, newWidth, newHeight), stretchedBitmap.BackBuffer, stretchedBitmap.BackBufferStride * newHeight, stretchedBitmap.BackBufferStride);
                stretchedBitmap.Unlock();

                return stretchedBitmap;
            }

            set {
                _fillImage = value;
                if (value != null) {
                    _fillImageBytes = value.SerializeToArray();
                }

                OnPropertyChanged(nameof(FillImage));
                RefreshableContainer.Refresh();
            }
        }

        public ICommand LoadFillImageCommand { get; protected set; }
        public void LoadFillImage() {
            Console.WriteLine("Image fill?");

            OpenFileDialog openFileDialog = new OpenFileDialog {
                Title = "Open",
                Filter = "Image Files (*.jpg *.jpeg *.png *.tiff)|*.jpg; *.jpeg; *.png; *.tiff|JPEG Files (*.jpg *.jpeg)|*.jpg; *.jpeg|PNG Files (*.png)|*.png|Tiff Files (*.tiff)|*.tiff"
            };

            if (openFileDialog.ShowDialog() == true) {
                try {
                    FillImage = new WriteableBitmap(new BitmapImage(new Uri(openFileDialog.FileName)));
                } catch (Exception ex) {
                    ex.DisplayAsMessageBox("Couldn't load image from filename");
                }
            }

            OnPropertyChanged(nameof(FillImage));
            RefreshableContainer.Refresh();
        }

        [DataMember]
        public SelectableColor FillColor { get; set; }

        public override string VerboseName => $"{BaseName}??";

        public override BaseBoundedCoord[] ClickableCoords {
            get {
                List<BaseBoundedCoord> coords = new List<BaseBoundedCoord>(Middle.ClickableCoords); // middle and vertices

                foreach (Line line in DrawLines()) {
                    coords.Add(line.Middle.CenterCoord);
                }

                return coords.ToArray();
            }
        }

        protected bool ExtendSetupQueueWhileUnclosed { get; set; }
        public override BaseBoundedCoord GetNextSetupCoord(System.Windows.Point? lastMouseDown) {
            if (ExtendSetupQueueWhileUnclosed) {
                if (!(lastMouseDown?.IsWithinRadiusOf(Vertices.First().AsPoint, ClickableCoordRadius) ?? false)) {
                    BoundedCoord newCoord = new BoundedCoord(this, -2, (0, ContainerSize.Item1), -2, (0, ContainerSize.Item2));

                    Vertices.Add(newCoord);
                    (Middle.CoordController.X as NamedBoundedValueController).ControlledValues.Add(newCoord.X as NamedBoundedValue);
                    (Middle.CoordController.Y as NamedBoundedValueController).ControlledValues.Add(newCoord.Y as NamedBoundedValue);
                    CoordSetupQueue.Enqueue(newCoord);
                } else {
                    // TODO: something about closing the shape properly??
                }
            }

            return base.GetNextSetupCoord(lastMouseDown);
        }
        #endregion

        #region creation
        protected Polygon(IRefreshableContainer refreshableContainer, DataContractSerializer serializer, string name, bool extendSetupQueueWhileUnclosed, SelectableColor color, SelectableColor fillColor = null)
            : base(refreshableContainer, serializer, name) {
            Color = new SelectableColor(this, color.SelectedColor);
            LoadFillImageCommand = new RelayCommand(LoadFillImage);
            ExtendSetupQueueWhileUnclosed = extendSetupQueueWhileUnclosed;
            FillColor = fillColor?.Clone() as SelectableColor ?? new SelectableColor(this, System.Drawing.Color.Transparent);
        }

        public Polygon(IRefreshableContainer refreshableContainer, DataContractSerializer serializer, VertexPointController vertexPointController, SelectableColor color, string name = nameof(Polygon), SelectableColor fillColor = null)
            : this(refreshableContainer, serializer, name, true, color, fillColor) {
            HashSet<BoundedCoord> copiedVerts = new HashSet<BoundedCoord>();
            foreach (BoundedCoord coord in vertexPointController.CoordController.ControlledCoords) {
                copiedVerts.Add((BoundedCoord)coord.Clone());
            }
            Middle = new VertexPointController(this, null, Color, "Middle", copiedVerts, MoveDirection.BOTH);

            foreach (BoundedCoord vertex in copiedVerts) {
                CoordSetupQueue.Enqueue(vertex);
            }
        }

        public Polygon(Polygon polygon)
            : this(polygon.RefreshableContainer,
                  polygon.ShapeSerializer,
                  polygon.Middle,
                  polygon.Color,
                  polygon.BaseName,
                  polygon.FillColor) {
            IsFilled = polygon.IsFilled;
            RenderSettingsProvider = polygon.RenderSettingsProvider;

            //FillImage = polygon.FillImage;
            FillImageBytes = polygon.FillImageBytes;
        }

        public override object Clone()
            => new Polygon(this);
        #endregion
    }
}
