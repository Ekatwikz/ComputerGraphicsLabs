using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Shapes;

namespace WPFDrawing {
    [DataContract]
    public class Rectangle : Polygon {
        #region stuff
        public override List<Line> DrawLines(List<RGBCoord> pixels = null) {
            List<Line> lines = new List<Line>();

            BoundedCoord firstCoord = Vertices.First();
            BoundedCoord lastCoord = Vertices.Last();

            VertexPoint firstPoint = new VertexPoint(firstCoord, false);
            VertexPoint thirdPoint = new VertexPoint(lastCoord, false);

            VertexPoint secondPoint = new VertexPoint(new BoundedCoord(this, firstCoord.X.Value, (0, firstCoord.Bounds.Item1), lastCoord.Y.Value, (0, lastCoord.Bounds.Item2)), false);
            VertexPoint fourthPoint = new VertexPoint(new BoundedCoord(this, lastCoord.X.Value, (0, lastCoord.Bounds.Item1), firstCoord.Y.Value, (0, firstCoord.Bounds.Item2)), false);

            lines.Add(new Line(this, null, Color, MoveDirection.HORIZONTAL, firstPoint, secondPoint) {
                RenderSettingsProvider = RenderSettingsProvider,
                Thickness = Thickness
            });
            lines.Add(new Line(this, null, Color, MoveDirection.HORIZONTAL, thirdPoint, fourthPoint) {
                RenderSettingsProvider = RenderSettingsProvider,
                Thickness = Thickness
            });
            lines.Add(new Line(this, null, Color, MoveDirection.VERTICAL, firstPoint, fourthPoint) {
                RenderSettingsProvider = RenderSettingsProvider,
                Thickness = Thickness
            });
            lines.Add(new Line(this, null, Color, MoveDirection.VERTICAL, secondPoint, thirdPoint) {
                RenderSettingsProvider = RenderSettingsProvider,
                Thickness = Thickness
            });

            if (pixels != null) {
                foreach (Line line in lines) {
                    foreach (RGBCoord coord in line.PixelCoords) {
                        pixels.Add(coord);
                    }
                }
            }

            return lines;
        }

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
        #endregion

        #region creation
        // TODO: this is almost entirely copy-pasted aside from one boolean, needs cleanup.
        public Rectangle(IRefreshableContainer refreshableContainer, DataContractSerializer serializer, VertexPointController vertexPointController, SelectableColor color, string name = nameof(Rectangle), SelectableColor fillColor = null)
            : base(refreshableContainer, serializer, name, false, color, fillColor) {
            HashSet<BoundedCoord> copiedVerts = new HashSet<BoundedCoord>();
            foreach (BoundedCoord coord in vertexPointController.CoordController.ControlledCoords) {
                copiedVerts.Add((BoundedCoord)coord.Clone());
            }
            Middle = new VertexPointController(this, null, Color, "Middle", copiedVerts, MoveDirection.BOTH);

            foreach (BoundedCoord vertex in copiedVerts) {
                CoordSetupQueue.Enqueue(vertex);
            }
        }

        public Rectangle(Rectangle rectangle)
            : this(rectangle.RefreshableContainer,
                  rectangle.ShapeSerializer,
                  rectangle.Middle,
                  rectangle.Color,
                  rectangle.BaseName,
                  rectangle.FillColor) {
            RenderSettingsProvider = rectangle.RenderSettingsProvider;
            IsFilled = rectangle.IsFilled;
            FillImageBytes = rectangle.FillImageBytes;
        }

        public override object Clone()
            => new Rectangle(this);
        #endregion
    }
}
