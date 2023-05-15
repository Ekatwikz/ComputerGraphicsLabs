using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace WPFDrawing {
    [DataContract]
    public class Line : Shape {
        public override RGBCoord[] PixelCoords {
            get {
                List<RGBCoord> pixels = new List<RGBCoord>(9);

                foreach (RGBCoord coord in Middle.PixelCoords) {
                    pixels.Add(coord);
                }

                if (RenderSettingsProvider?.CurrentRenderSettings.HasFlag(RenderSettings.XiaolinAlias) ?? false) {
                    DrawXiaolinLine(pixels, (int)Start.X.Value, (int)Start.Y.Value, (int)End.X.Value, (int)End.Y.Value);
                } else {
                    DrawLine(pixels, (int)Start.X.Value, (int)Start.Y.Value, (int)End.X.Value, (int)End.Y.Value, Thickness.Value);
                }

                return pixels.ToArray();
            }
        }

        #region algo
        private void DrawLine(List<RGBCoord> pixels, int x1, int y1, int x2, int y2, double thickness) {
            bool horizontal = Math.Abs(x1 - x2) > Math.Abs(y1 - y2);
            bool steep = Math.Abs(y2 - y1) > Math.Abs(x2 - x1);
            if (steep) {
                Swap(ref x1, ref y1);
                Swap(ref x2, ref y2);
            }
            if (x1 > x2) {
                Swap(ref x1, ref x2);
                Swap(ref y1, ref y2);
            }
            int dx = x2 - x1;
            int dy = Math.Abs(y2 - y1);
            int d = dy * 2 - dx;
            int y = y1;
            int ystep = (y1 < y2) ? 1 : -1;
            for (int x = x1; x <= x2; x++) {
                if (steep) {
                    AddThick(pixels, new RGBCoord { Coord = (y, x), CoordColor = Color.SelectedColor }, thickness, horizontal);
                } else {
                    AddThick(pixels, new RGBCoord { Coord = (x, y), CoordColor = Color.SelectedColor }, thickness, horizontal);
                }
                if (d > 0) {
                    y += ystep;
                    d -= dx * 2;
                }
                d += dy * 2;
            }
        }

        private void AddThick(List<RGBCoord> pixels, RGBCoord coord, double thickness, bool horizontal) {
            int thicc = (int)thickness;
            for (int i = -thicc; i < thicc; ++i) {
                if (horizontal) {
                    pixels.Add(new RGBCoord {
                        Coord = (coord.Coord.Item1, coord.Coord.Item2 + i),
                        CoordColor = coord.CoordColor
                    });
                } else {
                    pixels.Add(new RGBCoord {
                        Coord = (coord.Coord.Item1 + i, coord.Coord.Item2),
                        CoordColor = coord.CoordColor
                    });
                }
            }
        }

        public void DrawXiaolinLine(List<RGBCoord> pixels, int x1, int y1, int x2, int y2) {
            bool steep = Math.Abs(y2 - y1) > Math.Abs(x2 - x1);
            if (steep) {
                Swap(ref x1, ref y1);
                Swap(ref x2, ref y2);
            }
            if (x1 > x2) {
                Swap(ref x1, ref x2);
                Swap(ref y1, ref y2);
            }

            int dx = x2 - x1;
            int dy = Math.Abs(y2 - y1);
            int error = dx / 2;
            int ystep = (y1 < y2) ? 1 : -1;
            int y = y1;

            System.Drawing.Color Color1 = Color.SelectedColor;
            System.Drawing.Color Color2 = System.Drawing.Color.FromName("Transparent"); // ??

            for (int x = x1; x <= x2; x++) {
                float gradient;
                if (x == x1) {
                    gradient = 0;
                } else if (y == y1) {
                    gradient = float.MaxValue;
                } else if (steep) {
                    gradient = (float)(x - x1) / (y - y1);
                } else {
                    gradient = (float)(y - y1) / (x - x1);
                }

                if (steep) {
                    pixels.Add(new RGBCoord {
                        Coord = (y, x),
                        CoordColor = InterpolateColor(Color1, Color2, 1 - (gradient % 1))
                    });
                    pixels.Add(new RGBCoord {
                        Coord = (y + 1, x),
                        CoordColor = InterpolateColor(Color1, Color2, gradient % 1)
                    });
                } else {
                    pixels.Add(new RGBCoord {
                        Coord = (x, y),
                        CoordColor = InterpolateColor(Color1, Color2, 1 - (gradient % 1))
                    });
                    pixels.Add(new RGBCoord {
                        Coord = (x, y + 1),
                        CoordColor = InterpolateColor(Color1, Color2, gradient % 1)
                    });
                }

                error -= dy;
                if (error < 0) {
                    y += ystep;
                    error += dx;
                }
            }
        }

        private static System.Drawing.Color InterpolateColor(System.Drawing.Color color1, System.Drawing.Color color2, double fraction) {
            if (fraction > 1) {
                fraction = 2 - fraction;
            }

            if (fraction < 0) {
                fraction = -fraction;
            }

            return System.Drawing.Color.FromArgb((byte)(color1.A + (color2.A - color1.A) * fraction),
                              (byte)(color1.R + (color2.R - color1.R) * fraction),
                              (byte)(color1.G + (color2.G - color1.G) * fraction),
                              (byte)(color1.B + (color2.B - color1.B) * fraction));
        }
        #endregion

        #region stuff
        public override string VerboseName => $"{BaseName}??";

        private VertexPointController _middle;
        [DataMember]
        public VertexPointController Middle {
            get => _middle;
            private set {
                _middle = value;
                _middle.RefreshableContainer = this;
            }
        }

        public BaseBoundedCoord Start => Middle.CoordController.ControlledCoords.ToArray()[0];
        public BaseBoundedCoord End => Middle.CoordController.ControlledCoords.ToArray()[1];
        public double Length => Start.AsPoint.DistanceFrom(End.AsPoint);

        public override BaseBoundedCoord[] ClickableCoords => Middle.ClickableCoords;
        #endregion

        #region creation
        private Line(IRefreshableContainer refreshableContainer, DataContractSerializer shapeSerializer, string baseName)
            : base(refreshableContainer, shapeSerializer, baseName) { }

        public Line(IRefreshableContainer refreshableContainer, DataContractSerializer shapeSerializer, SelectableColor color, VertexPointController vertexPointController, string baseName = "Line")
            : this(refreshableContainer, shapeSerializer, baseName) {
            Color = new SelectableColor(this, color.SelectedColor);

            Middle = (VertexPointController)vertexPointController.Clone();

            BoundedCoord[] coords = vertexPointController.CoordController.ControlledCoords.ToArray();
            CoordSetupQueue.Enqueue(coords[0]);
            CoordSetupQueue.Enqueue(coords[1]);
        }

        public Line(IRefreshableContainer refreshableContainer, DataContractSerializer shapeSerializer, SelectableColor color, MoveDirection moveDirection, VertexPoint start = null, VertexPoint end = null, string baseName = "Line")
            : this(refreshableContainer,
                  shapeSerializer, color,
                  new VertexPointController(null, shapeSerializer, color, "middle",
                      moveDirection,
                      start ?? new VertexPoint(null, shapeSerializer),
                      end ?? new VertexPoint(null, shapeSerializer)),
                  baseName) { }

        public Line(IRefreshableContainer refreshableContainer, MoveDirection moveDirection, DataContractSerializer shapeSerializer = null)
            : this(refreshableContainer, shapeSerializer, new SelectableColor(null, "Purple"),
                  moveDirection,
                  new VertexPoint(refreshableContainer, shapeSerializer), new VertexPoint(refreshableContainer, shapeSerializer) // nonsense
                  ) { }

        public Line(Line line)
            : this(line.RefreshableContainer,
                  line.ShapeSerializer,
                  line.Color,
                  line.Middle.CoordController.Direction,
                  new VertexPoint(line.Middle.CoordController.ControlledCoords.ToArray()[0]), // Yikes...
                  new VertexPoint(line.Middle.CoordController.ControlledCoords.ToArray()[1]),
                  line.BaseName) {
            RenderSettingsProvider = line.RenderSettingsProvider;
        }

        public override object Clone()
            => new Line(this);
        #endregion
    }
}
