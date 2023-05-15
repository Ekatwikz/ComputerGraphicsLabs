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
    public enum CohenOutcode {
        LEFT = 1,
        RIGHT,
        BOTTOM = 4,
        TOP = 8
    }

    public struct ETEdge {
        public double Ymax,
            X;
        public double InverseSlope;
    }

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

                DrawLines(pixels); // TMP!! PUT BACK!!

                foreach (Line line in ClipHighlighterLines()) {
                    foreach (RGBCoord coord in line.PixelCoords) {
                        pixels.Add(coord.Invert());
                    }
                }

                return pixels.ToArray();
            }
        }

        #region clipAndFill
        private CohenOutcode GetCohenOutcode(Point point) {
            CohenOutcode outcode = 0;

            if (ClippingRect == null) {
                return outcode;
            }

            ((double, double), (double, double)) clipBox = ClippingRectBoundingBox;
            double xMin = clipBox.Item1.Item1,
                xMax = clipBox.Item1.Item2,
                yMin = clipBox.Item2.Item1,
                yMax = clipBox.Item2.Item2;

            if (point.X > xMax) outcode |= CohenOutcode.RIGHT;
            else if (point.X < xMin) outcode |= CohenOutcode.LEFT;

            if (point.Y > yMax) outcode |= CohenOutcode.TOP;
            else if (point.Y < yMin) outcode |= CohenOutcode.BOTTOM;

            return outcode;
        }

        // Cohen–Sutherland clipping algorithm clips A line from
        // P0 = (x0, y0) to P1 = (x1, y1) against A rectangle with
        // diagonal from (xMin, yMin) to (xMax, yMax).
        private bool CohenClip(ref Point pointA, ref Point pointB) {
            // compute outcodes for P0, P1, and whatever point lies outside the clip rectangle
            CohenOutcode outcodeA = GetCohenOutcode(pointA);
            CohenOutcode outcodeB = GetCohenOutcode(pointB);

            bool accept = false;

            ((double, double), (double, double)) clipBox = ClippingRectBoundingBox;
            double xMin = clipBox.Item1.Item1,
                xMax = clipBox.Item1.Item2,
                yMin = clipBox.Item2.Item1,
                yMax = clipBox.Item2.Item2;

            while (true) {
                if (!(outcodeA | outcodeB).ToBool()) {
                    // bitwise OR is 0: both points inside window; trivially accept and exit loop
                    accept = true;
                    break;
                } else if ((outcodeA & outcodeB).ToBool()) {
                    // bitwise AND is not 0: both points share an outside zone (LEFT, RIGHT, TOP,
                    // or BOTTOM), so both must be outside window; exit loop (accept is false)
                    break;
                } else {
                    // failed both tests, so calculate the line segment to clip
                    // from an outside point to an intersection with clip line
                    //double x, y;
                    Point point = new Point { };

                    // At least one endpoint is outside the clip rectangle; pick it.
                    CohenOutcode outcodeOut = outcodeB > outcodeA ? outcodeB : outcodeA;

                    // Now find the intersection point;
                    // use formulas:
                    //   slope = (y1 - y0) / (x1 - x0)
                    //   x = x0 + (1 / slope) * (ym - y0), where ym is yMin or yMax
                    //   y = y0 + slope * (xm - x0), where xm is xMin or xMax
                    // No need to worry about divide-by-zero because, in each case, the
                    // outcode bit being tested guarantees the denominator is non-zero
                    if ((outcodeOut & CohenOutcode.TOP).ToBool()) {           // point is above the clip window
                        point.X = pointA.X + (pointB.X - pointA.X) * (yMax - pointA.Y) / (pointB.Y - pointA.Y);
                        point.Y = yMax;
                    } else if ((outcodeOut & CohenOutcode.BOTTOM).ToBool()) { // point is below the clip window
                        point.X = pointA.X + (pointB.X - pointA.X) * (yMin - pointA.Y) / (pointB.Y - pointA.Y);
                        point.Y = yMin;
                    } else if ((outcodeOut & CohenOutcode.RIGHT).ToBool()) {  // point is to the right of clip window
                        point.Y = pointA.Y + (pointB.Y - pointA.Y) * (xMax - pointA.X) / (pointB.X - pointA.X);
                        point.X = xMax;
                    } else if ((outcodeOut & CohenOutcode.LEFT).ToBool()) {   // point is to the left of clip window
                        point.Y = pointA.Y + (pointB.Y - pointA.Y) * (xMin - pointA.X) / (pointB.X - pointA.X);
                        point.X = xMin;
                    }

                    // Now we move outside point to intersection point to clip
                    // and get ready for next pass.
                    if (outcodeOut == outcodeA) {
                        pointA.X = point.X;
                        pointA.Y = point.Y;
                        outcodeA = GetCohenOutcode(pointA);
                    } else {
                        pointB.X = point.X;
                        pointB.Y = point.Y;
                        outcodeB = GetCohenOutcode(pointB);
                    }
                }
            }

            return accept;
        }

        public List<Line> ClipHighlighterLines() {
            List<Line> lines = new List<Line>();

            if (ClippingRect == null) {
                return lines;
            }

            foreach (Line line in DrawLines()) {
                Point pointA = line.Start.AsPoint,
                    pointB = line.End.AsPoint;
                if (CohenClip(ref pointA, ref pointB)) {
                    lines.Add(new Line(this, null, Color, MoveDirection.BOTH,
                        new VertexPoint(new BoundedCoord(this,
                            pointA.X,
                            (0, Bounds.Item1),
                            pointA.Y,
                            (0, Bounds.Item2)), false),
                        new VertexPoint(new BoundedCoord(this,
                            pointB.X,
                            (0, Bounds.Item1),
                            pointB.Y,
                            (0, Bounds.Item2)), false)) {
                        RenderSettingsProvider = RenderSettingsProvider,
                        Thickness = Thickness
                    });
                }
            }

            return lines;
        }

        private (List<List<ETEdge>>, int) GetEdgeTable() {
            ((double, double), (double, double)) boundingBox = BoundingBox;

            // TODO: SHOULD THESE BE FLOOR/CEIL-d ??
            int yMin = (int)boundingBox.Item2.Item1,
                yMax = (int)boundingBox.Item2.Item2;

            // Create an empty ETEdge Table (ET)
            List<List<ETEdge>> ET = new List<List<ETEdge>>(yMax - yMin + 1);
            for (int i = 0; i < yMax - yMin + 1; ++i) {
                ET.Add(new List<ETEdge>());
            }

            double overall_line_yMin = double.MaxValue;
            // Iterate over each line of the polygon
            foreach (Line line in DrawLines()) {
                ((double, double), (double, double)) lineBoundingBox = line.BoundingBox();
                double line_xMin = lineBoundingBox.Item1.Item1, // TODO: remove this comment if this actually works lol?
                    line_xMax = lineBoundingBox.Item1.Item2,
                    line_yMin = lineBoundingBox.Item2.Item1,
                    line_yMax = lineBoundingBox.Item2.Item2;
                overall_line_yMin = Math.Min(overall_line_yMin, line_yMin);

                // Calculate the inverse of the slope (1/m) of the line
                double inverseSlope = (line.End.AsPoint.X - line.Start.AsPoint.X) / (line.End.AsPoint.Y - line.Start.AsPoint.Y);
                inverseSlope = double.IsInfinity(inverseSlope) ? 0 : inverseSlope;

                // Create an line entry and insert it into the corresponding ET bucket
                ETEdge entry = new ETEdge {
                    Ymax = line_yMax,
                    X = inverseSlope > 0 ? line_xMin : line_xMax, // WEIRD HACK!!, DODES THIS ACTUALLY WORK??
                    InverseSlope = inverseSlope
                };

                ET[(int)line_yMin - yMin].Add(entry);
            }

            return (ET, (int)overall_line_yMin);
        }

        private void Fill(List<RGBCoord> pixels) {
            (List<List<ETEdge>> ET, int overall_line_yMin) = GetEdgeTable();
            List<ETEdge> AET = new List<ETEdge>();

            int i = 0;
            int y = overall_line_yMin;

            WriteableBitmap fillImage = FillImage;
            byte[] fillImagePixelBuffer = FillImagePixelBuffer;

            while (i == 0 || AET.Count > 0 && i < ET.Count) {
                foreach (ETEdge edge in ET[i]) {
                    AET.Add(edge);
                }

                AET.Sort((a, b) => a.X.CompareTo(b.X));

                if (FillImage == null) {
                    for (int j = 1; j < AET.Count; j += 2) {
                        for (double k = AET[j - 1].X; k < AET[j].X; ++k) {
                            pixels.Add(new RGBCoord {
                                Coord = (k.ClipToInt(), y),
                                CoordColor = FillColor.SelectedColor,
                            });
                        }
                    }
                } else {
                    ((double, double), (double, double)) boundingBox = BoundingBox;
                    int xMin = (int)boundingBox.Item1.Item1,
                        yMin = (int)boundingBox.Item2.Item1;

                    for (int j = 1; j < AET.Count; j += 2) {
                        for (double k = AET[j - 1].X; k < AET[j].X; ++k) {
                            int x = k.ClipToInt();
                            int index = (y - yMin) * fillImage.BackBufferStride + (x - xMin) * (fillImage.Format.BitsPerPixel / 8);
                            if (index < 0) {
                                continue;
                            }

                            if (index >= fillImagePixelBuffer.Length) {
                                continue;
                            }

                            byte R = fillImagePixelBuffer[index + 2];
                            byte G = fillImagePixelBuffer[index + 1];
                            byte B = fillImagePixelBuffer[index];
                            byte A = fillImagePixelBuffer[index + 3];

                            pixels.Add(new RGBCoord {
                                Coord = (x, y),
                                CoordColor = System.Drawing.Color.FromArgb(A, R, G, B),
                            });
                        }
                    }
                }

                ++y;
                AET.RemoveAll(edge => (int)edge.Ymax == y);

                for (int j = 0; j < AET.Count; ++j) {
                    ETEdge edge = AET[j];
                    edge.X += edge.InverseSlope; // flat-ish lines?
                    AET[j] = edge;
                }

                ++i;
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

        private ((double, double), (double, double)) _boundingBox; // cheap hax everywhere
        [DataMember]
        public ((double, double), (double, double)) BoundingBox {
            get {
                if (_boundingBox != ((0, 0), (0, 0))) {
                    return _boundingBox;
                }

                double xMin = double.MaxValue,
                xMax = double.MinValue,
                yMin = double.MaxValue,
                yMax = double.MinValue;

                foreach (Line line in DrawLines()) {
                    foreach (BoundedCoord coord in line.Middle.CoordController.ControlledCoords) {
                        xMin = Math.Min(xMin, coord.AsPoint.X);
                        xMax = Math.Max(xMax, coord.AsPoint.X);

                        yMin = Math.Min(yMin, coord.AsPoint.Y);
                        yMax = Math.Max(yMax, coord.AsPoint.Y);
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

            set {
                _boundingBox = value;
            }
        }

        public Rectangle ClippingRect { get; set; }
        public ((double, double), (double, double)) ClippingRectBoundingBox => ClippingRect.BoundingBox;

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

        public override void RefreshHook() {
            if (_fillImage != null && IsFilled) {
                StretchFillImage();
                UpdatePixelBuffer();
            }
        }

        private void UpdatePixelBuffer() {
            if (_fillImage != null) {
                FillImagePixelBuffer = new byte[FillImage.BackBufferStride * FillImage.PixelHeight];
                FillImage.CopyPixels(FillImagePixelBuffer, FillImage.BackBufferStride, 0);
            } else {
                Console.WriteLine("Updatin nuffink??");
            }
        }

        private byte[] FillImagePixelBuffer { get; set; }

        private byte[] _fillImageBytes;
        [DataMember]
        public byte[] FillImageBytes {
            get => _fillImageBytes;
            set {
                _fillImageBytes = value;
                if (value != null) {
                    _fillImage = _fillImageBytes.DeserializeToWriteableBitmap();
                    StretchFillImage();
                    UpdatePixelBuffer();
                }

                OnPropertyChanged(nameof(FillImageBytes));
                RefreshableContainer?.Refresh();
            }
        }

        private void StretchFillImage() {
            if (_fillImage == null) {
                _fillImageStretched = null;
                return;
            }

            ((double, double), (double, double)) boundingBox = BoundingBox;
            int newWidth = (int)(boundingBox.Item1.Item2 - boundingBox.Item1.Item1 + 1); // Your new width
            int newHeight = (int)(boundingBox.Item2.Item2 - boundingBox.Item2.Item1 + 1); // Your new height
            if (newWidth == 0 || newHeight == 0) {
                _fillImageStretched = null; // (usually?) when rectangle is in setup mode
                return;
            }

            // Create A new WriteableBitmap with the new dimensions
            //WriteableBitmap stretchedBitmap = new WriteableBitmap(newWidth, newHeight, _fillImage.DpiX, _fillImage.DpiY, PixelFormats.Default, null);
            WriteableBitmap stretchedBitmap = new WriteableBitmap(newWidth, newHeight, _fillImage.DpiX, _fillImage.DpiY, _fillImage.Format, null);

            // Use A DrawingVisual and RenderTargetBitmap to render the original bitmap onto the new one, scaled to fit
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

            _fillImageStretched = stretchedBitmap;
        }

        private WriteableBitmap _fillImage;
        private WriteableBitmap _fillImageStretched;
        protected WriteableBitmap FillImage {
            get => _fillImageStretched;

            set {
                _fillImage = value;
                if (value != null) {
                    _fillImageBytes = value.SerializeToArray();
                }

                StretchFillImage();
                UpdatePixelBuffer();

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

            FillImageBytes = polygon.FillImageBytes;
        }

        public override object Clone()
            => new Polygon(this);
        #endregion
    }
}
