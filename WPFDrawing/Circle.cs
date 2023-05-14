using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace WPFDrawing {
    [DataContract]
    public class Circle : Shape {
        public override RGBCoord[] PixelCoords {
            get {
                List<RGBCoord> pixels = new List<RGBCoord>(9);

                if (RenderSettingsProvider.CurrentRenderSettings.HasFlag(RenderSettings.XiaolinAlias)) {
                    DrawXiaolinWuCircle(pixels, (int)Radius);
                } else {
                    DrawMidpointCircle(pixels, (int)Radius);

                    double partial = Partial.Value; // TODO: actually use this properly

                    //if (partial < 1) {
                    if (false) { // tmp for lab part
                        // Filter out the points that are not within the angle range
                        List<RGBCoord> newPixels = new List<RGBCoord>();
                        foreach (RGBCoord colorCoord in pixels) {
                            double angle = Math.Atan2(colorCoord.Coord.Item2 - Center.Y.Value, colorCoord.Coord.Item1 - Center.X.Value);

                            // Calculate the angle of the line
                            double lineAngle = Math.Atan2(Diameter.End.X.Value - Diameter.Start.X.Value, Diameter.End.Y.Value - Diameter.Start.Y.Value);

                            // Define the angle range based on the line slope
                            double startAngle = lineAngle - Math.PI / 2.0;
                            double endAngle = lineAngle + Math.PI / 2.0;

                            if (angle >= startAngle && angle <= endAngle) {
                                newPixels.Add(colorCoord);
                            }
                        }

                        pixels = newPixels;
                    }
                }

                foreach (RGBCoord coord in Diameter.PixelCoords) {
                    pixels.Add(coord);
                }

                return pixels.ToArray();
            }
        }

        private void DrawMidpointCircle(List<RGBCoord> pixels, int R) {
            int dE = 3;
            int dSE = 5 - 2 * R;
            int d = 1 - R;
            int x = 0;
            int y = R;

            int offX = (int)Center.X.Value;
            int offY = (int)Center.Y.Value;

            DrawCirclePoints(pixels, x, y, offX, offY, Color.SelectedColor);

            while (y > x) {
                if (d < 0)
                //move to E
                {
                    d += dE;
                    dE += 2;
                    dSE += 2;
                } else
                  //move to SE
                  {
                    d += dSE;
                    dE += 2;
                    dSE += 4;
                    --y;
                }

                ++x;
                DrawCirclePoints(pixels, x, y, offX, offY, Color.SelectedColor);
            }
        }

        private void DrawCirclePoints(List<RGBCoord> pixels, int x, int y, int offX, int offY, System.Drawing.Color color) {
            pixels.Add(new RGBCoord { Coord = (x + offX, y + offY), CoordColor = color });
            pixels.Add(new RGBCoord { Coord = (-x + offX, y + offY), CoordColor = color });

            pixels.Add(new RGBCoord { Coord = (x + offX, -y + offY), CoordColor = color });
            pixels.Add(new RGBCoord { Coord = (-x + offX, -y + offY), CoordColor = color });

            pixels.Add(new RGBCoord { Coord = (y + offX, x + offY), CoordColor = color });
            pixels.Add(new RGBCoord { Coord = (-y + offX, x + offY), CoordColor = color });

            pixels.Add(new RGBCoord { Coord = (y + offX, -x + offY), CoordColor = color });
            pixels.Add(new RGBCoord { Coord = (-y + offX, -x + offY), CoordColor = color });
        }

        private void DrawXiaolinWuCircle(List<RGBCoord> pixels, int R) {
            int offX = (int)Center.X.Value;
            int offY = (int)Center.Y.Value;
            double x = R;
            double y = 0;
            double cx = offX;
            double cy = offY;


            // Draw the center pixel
            DrawCirclePixel(pixels, offX, offY, 1, Color.SelectedColor);

            while (x > y) {
                // Calculate brightness of pixels along the circle
                double brightness = 1 - (x - Math.Floor(x));

                // Draw the eight symmetric pixels
                DrawCirclePixel(pixels, (int)Math.Floor(x + cx), (int)Math.Floor(y + cy), brightness, Color.SelectedColor);
                DrawCirclePixel(pixels, (int)Math.Floor(y + cx), (int)Math.Floor(x + cy), brightness, Color.SelectedColor);
                DrawCirclePixel(pixels, (int)Math.Floor(-x + cx), (int)Math.Floor(y + cy), brightness, Color.SelectedColor);
                DrawCirclePixel(pixels, (int)Math.Floor(-y + cx), (int)Math.Floor(x + cy), brightness, Color.SelectedColor);
                DrawCirclePixel(pixels, (int)Math.Floor(-x + cx), (int)Math.Floor(-y + cy), brightness, Color.SelectedColor);
                DrawCirclePixel(pixels, (int)Math.Floor(-y + cx), (int)Math.Floor(-x + cy), brightness, Color.SelectedColor);
                DrawCirclePixel(pixels, (int)Math.Floor(x + cx), (int)Math.Floor(-y + cy), brightness, Color.SelectedColor);
                DrawCirclePixel(pixels, (int)Math.Floor(y + cx), (int)Math.Floor(-x + cy), brightness, Color.SelectedColor);

                // Update the x and y values
                y += 1;
                x = Math.Sqrt(R * R - y * y);
            }

        }

        private void DrawCirclePixel(List<RGBCoord> pixels, int x, int y, double brightness, System.Drawing.Color color) {
            // Calculate the RGB values based on the brightness and selected color
            int r = (int)(brightness * color.R);
            int g = (int)(brightness * color.G);
            int b = (int)(brightness * color.B);

            // Add the pixel to the list of pixels to draw
            pixels.Add(new RGBCoord { Coord = (x, y), CoordColor = System.Drawing.Color.FromArgb(r, g, b) });
        }

        #region stuff
        public NamedBoundedValue Partial { get; set; }

        private Line _diameter;
        [DataMember]
        public Line Diameter {
            get => _diameter;
            private set {
                _diameter = value;
                _diameter.RefreshableContainer = this;
            }
        }

        public BaseBoundedCoord Center => Diameter.Middle.CenterCoord;
        public BoundedCoord EdgePoint => Diameter.Middle.CoordController.ControlledCoords.First();
        public double Radius => Center.AsPoint.DistanceFrom(EdgePoint.AsPoint);

        public override string VerboseName => $"{BaseName}??";

        public override BaseBoundedCoord[] ClickableCoords => Diameter.Middle.ClickableCoords;
        #endregion

        #region
        private Circle(IRefreshableContainer refreshableContainer, DataContractSerializer shapeSerializer, string baseName)
            : base(refreshableContainer, shapeSerializer, baseName) {
            Partial = new NamedBoundedValue("Partial", 0.5, (0, 1));
        }

        public Circle(IRefreshableContainer refreshableContainer, DataContractSerializer shapeSerializer, SelectableColor color, Line diameter, string baseName = "Circle")
            : this(refreshableContainer, shapeSerializer, baseName) {
            Color = new SelectableColor(this, color.SelectedColor);
            Diameter = (Line)diameter.Clone();

            CoordSetupQueue.Enqueue(Diameter.Middle.CenterCoord);
            CoordSetupQueue.Enqueue(Diameter.Middle.CoordController.ControlledCoords.First());
        }

        public Circle(IRefreshableContainer refreshableContainer, DataContractSerializer shapeSerializer)
            : this(refreshableContainer, shapeSerializer, new SelectableColor("Orange"), new Line(null, null)) { }

        public Circle(Circle circle)
            : this(circle.RefreshableContainer,
                  circle.ShapeSerializer,
                  circle.Color,
                  circle.Diameter,
                  circle.BaseName) {
            RenderSettingsProvider = circle.RenderSettingsProvider;
            Diameter.RenderSettingsProvider = circle.RenderSettingsProvider;
        }

        public override object Clone()
            => new Circle(this);
        #endregion
    }
}
