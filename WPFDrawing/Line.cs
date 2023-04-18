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

                DrawXiaolinLine4(pixels, (int)Start.X.Value, (int)Start.Y.Value, (int)End.X.Value, (int)End.Y.Value);

                return pixels.ToArray();
            }
        }

        #region algo

        private void DrawLine(List<RGBCoord> pixels, int x1, int y1, int x2, int y2) {
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
                    pixels.Add(new RGBCoord { Coord = (y, x), CoordColor = Color.SelectedColor });
                } else {
                    pixels.Add(new RGBCoord { Coord = (x, y), CoordColor = Color.SelectedColor });
                }
                if (d > 0) {
                    y += ystep;
                    d -= dx * 2;
                }
                d += dy * 2;
            }
        }

        public void DrawXiaolinLine4(List<RGBCoord> pixels, int x1, int y1, int x2, int y2) {
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
            System.Drawing.Color Color2 = System.Drawing.Color.FromName("White"); // ??

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

                //if (steep) {
                //    pixels.Add(new RGBCoord {
                //        Coord = (y, x),
                //        CoordColor = InterpolateColors(1 - (gradient % 1), Color1.SelectedColor, Color2.SelectedColor)
                //    });
                //    pixels.Add(new RGBCoord {
                //        Coord = (y + 1, x),
                //        CoordColor = InterpolateColors(gradient % 1, Color1.SelectedColor, Color2.SelectedColor)
                //    });
                //} else {
                //    pixels.Add(new RGBCoord {
                //        Coord = (x, y),
                //        CoordColor = InterpolateColors(1 - (gradient % 1), Color1.SelectedColor, Color2.SelectedColor)
                //    });
                //    pixels.Add(new RGBCoord {
                //        Coord = (x, y + 1),
                //        CoordColor = InterpolateColors(gradient % 1, Color1.SelectedColor, Color2.SelectedColor)
                //    });
                //}
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

        public void DrawXiaolinLine3(List<RGBCoord> pixels, int x1, int y1, int x2, int y2) {
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
            System.Drawing.Color Color2 = System.Drawing.Color.FromName("White"); // ??

            for (int x = x1; x <= x2; x++) {
                float gradient = (float)(y - y1) / (x - x1);
                if (float.IsNaN(gradient)) {
                    gradient = float.MaxValue;
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

        public void DrawXiaolinLine(List<RGBCoord> pixels, int x1, int y1, int x2, int y2) {
            System.Drawing.Color color = Color.SelectedColor;

            pixels.Add(new RGBCoord { Coord = (x1, y1), CoordColor = color });
            pixels.Add(new RGBCoord { Coord = (x2, y2), CoordColor = color });

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
            //int dy = Math.Abs(y2 - y1);
            //float gradient = (float)dy / (float)dx;
            //if (dx == 0) {
            //    gradient = 1;
            //}
            int xpxl1 = x1;
            int xpxl2 = x2;
            //float intersectY = y1;
            if (steep) {
                int x;
                for (x = xpxl1 + 1; x <= xpxl2 - 1; x++) {
                    float t = (float)(x - x1) / dx;
                    float y = y1 * (1 - t) + y2 * t;
                    int ypxl = (int)y;
                    float alpha = y - ypxl;
                    pixels.Add(new RGBCoord { Coord = (ypxl, x), CoordColor = InterpolateColor(color, alpha) });
                    pixels.Add(new RGBCoord { Coord = (ypxl + 1, x), CoordColor = InterpolateColor(color, 1 - alpha) });
                }
            } else {
                int x;
                for (x = xpxl1 + 1; x <= xpxl2 - 1; x++) {
                    float t = (float)(x - x1) / dx;
                    float y = y1 * (1 - t) + y2 * t;
                    int ypxl = (int)y;
                    float alpha = y - ypxl;
                    pixels.Add(new RGBCoord { Coord = (x, ypxl), CoordColor = InterpolateColor(color, alpha) });
                    pixels.Add(new RGBCoord { Coord = (x, ypxl + 1), CoordColor = InterpolateColor(color, 1 - alpha) });
                }
            }
        }

        private void DrawXiaolinLine2(List<RGBCoord> pixels, int x1, int y1, int x2, int y2) {
            // swap points to ensure x1 <= x2
            if (x1 > x2) {
                (x1, x2) = (x2, x1);
                (y1, y2) = (y2, y1);
            }

            // calculate differences and other values
            int dx = x2 - x1;
            int dy = y2 - y1;
            double slope = 0;
            if (dx != 0) {
                slope = (double)dy / dx;
            }

            // if slope is greater than 1, swap x and y values
            bool steep = Math.Abs(slope) > 1;
            if (steep) {
                (x1, y1) = (y1, x1);
                (x2, y2) = (y2, x2);
                dx = Math.Abs(y2 - y1);
                dy = Math.Abs(x2 - x1);
            }

            // calculate error values and other values needed for interpolation
            double error = dx / 2.0;
            int ystep = (y1 < y2) ? 1 : -1;
            int y = y1;

            for (int x = x1; x <= x2; x++) {
                double fraction = 0;
                if (dx != 0) {
                    fraction = (double)(x - x1) / dx;
                }

                // perform color interpolation
                var color1 = Color.SelectedColor;
                var color2 = System.Drawing.Color.FromName("White"); // ??
                if (steep) {
                    pixels.Add(new RGBCoord {
                        Coord = (y, x),
                        CoordColor = InterpolateColor(color1, color2, 1 - fraction)
                    });
                } else {
                    pixels.Add(new RGBCoord {
                        Coord = (x, y),
                        CoordColor = InterpolateColor(color1, color2, 1 - fraction)
                    });
                }

                // add anti-aliased pixels if slope is less than 1
                if (!steep && dx > 0) {
                    var alpha = (int)Math.Round(255 * (error / dx));
                    var color = InterpolateColor(Color.SelectedColor, System.Drawing.Color.FromName("White"), error / dx);
                    if (alpha > 0) {
                        pixels.Add(new RGBCoord {
                            Coord = (x, y + ystep),
                            CoordColor = System.Drawing.Color.FromArgb(alpha, color)
                        });
                    }
                    if (alpha < 255) {
                        pixels.Add(new RGBCoord {
                            Coord = (x, y),
                            CoordColor = System.Drawing.Color.FromArgb(255 - alpha, color)
                        });
                    }
                } else if (steep && dy > 0) {
                    // add anti-aliased pixels if slope is greater than 1
                    var alpha = (int)Math.Round(255 * (error / dy));
                    var color = InterpolateColor(Color.SelectedColor, System.Drawing.Color.FromName("White"), error / dy);
                    if (alpha > 0) {
                        pixels.Add(new RGBCoord {
                            Coord = (y + ystep, x),
                            CoordColor = System.Drawing.Color.FromArgb(alpha, color)
                        });
                    }
                    if (alpha < 255) {
                        pixels.Add(new RGBCoord {
                            Coord = (y, x),
                            CoordColor = System.Drawing.Color.FromArgb(255 - alpha, color)
                        });
                    }
                }
            }
        }

        private static System.Drawing.Color InterpolateColor(System.Drawing.Color color1, System.Drawing.Color color2, double fraction) {
            var r1 = color1.R;
            var g1 = color1.G;
            var b1 = color1.B;
            var a1 = color1.A;

            var r2 = color2.R;
            var g2 = color2.G;
            var b2 = color2.B;
            var a2 = color2.A;

            return System.Drawing.Color.FromArgb((a1 + (a2 - a1) * fraction).ClipToByte(),
                              (r1 + (r2 - r1) * fraction).ClipToByte(),
                              (g1 + (g2 - g1) * fraction).ClipToByte(),
                              (b1 + (b2 - b1) * fraction).ClipToByte());
        }

        private System.Drawing.Color InterpolateColor(System.Drawing.Color c, float alpha) {
            return System.Drawing.Color.FromArgb(
                (c.A * alpha).ClipToByte(),
                (c.R * alpha).ClipToByte(),
                (c.G * alpha).ClipToByte(),
                (c.B * alpha).ClipToByte());
        }
        #endregion

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

        protected IRenderSettingsProvider RenderSettingsProvider { get; set; }

        public BaseBoundedCoord Start => Middle.CoordController.ControlledCoords.ToArray()[0];
        public BaseBoundedCoord End => Middle.CoordController.ControlledCoords.ToArray()[1];

        public override BaseBoundedCoord[] ClickableCoords => Middle.ClickableCoords;

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

        public Line(IRefreshableContainer refreshableContainer, DataContractSerializer shapeSerializer, SelectableColor color, VertexPoint start = null, VertexPoint end = null, string baseName = "Line")
            : this(refreshableContainer,
                  shapeSerializer, color,
                  new VertexPointController(null, shapeSerializer, color, "middle",
                      start ?? new VertexPoint(null, shapeSerializer),
                      end ?? new VertexPoint(null, shapeSerializer)),
                  baseName) { }

        public Line(IRefreshableContainer refreshableContainer, DataContractSerializer shapeSerializer = null)
            : this(refreshableContainer, shapeSerializer, new SelectableColor(null, "Purple"),
                  new VertexPoint(refreshableContainer, shapeSerializer), new VertexPoint(refreshableContainer, shapeSerializer) // nonsense
                  ) { }

        public Line(Line line)
            : this(line.RefreshableContainer,
                  line.ShapeSerializer,
                  line.Color,
                  new VertexPoint(line.Middle.CoordController.ControlledCoords.ToArray()[0]), // Yikes...
                  new VertexPoint(line.Middle.CoordController.ControlledCoords.ToArray()[1]),
                  line.BaseName) { }

        public override object Clone()
            => new Line(this);
        #endregion
    }
}
