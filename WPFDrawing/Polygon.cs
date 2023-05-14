using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace WPFDrawing {
    [DataContract]
    public class Polygon : Shape {
        public override RGBCoord[] PixelCoords {
            get {
                List<RGBCoord> pixels = new List<RGBCoord>(9);

                foreach (RGBCoord coord in Middle.PixelCoords) {
                    pixels.Add(coord);
                }

                foreach (Line line in Lines) {
                    foreach (RGBCoord coord in line.PixelCoords) {
                        pixels.Add(coord);
                    }
                }

                return pixels.ToArray();
            }
        }

        #region stuff
        private VertexPointController _middle;
        [DataMember]
        public VertexPointController Middle {
            get => _middle;
            private set {
                _middle = value;
                _middle.RefreshableContainer = this;
            }
        }

        public HashSet<BoundedCoord> Vertices => Middle.CoordController.ControlledCoords;

        public List<Line> Lines {
            get {
                List<Line> lines = new List<Line>();

                BoundedCoord prevCoord = null;
                foreach (BoundedCoord vertex in Vertices) {
                    if (prevCoord != null) {
                        Line line = new Line(this, null, Color, new VertexPoint(prevCoord, false), new VertexPoint(vertex, false)) {
                            RenderSettingsProvider = RenderSettingsProvider,
                            Thickness = Thickness
                        };
                        lines.Add(line);
                    }

                    prevCoord = vertex;
                }

                if (Vertices.Count > 1) {
                    Line lastLine = new Line(this, null, Color, new VertexPoint(prevCoord, false), new VertexPoint(Vertices.First(), false)) {
                        RenderSettingsProvider = RenderSettingsProvider,
                        Thickness = Thickness
                    };
                    lines.Add(lastLine);
                }

                return lines;
            }
        }

        public override string VerboseName => $"{BaseName}??";

        public override BaseBoundedCoord[] ClickableCoords {
            get {
                List<BaseBoundedCoord> coords = new List<BaseBoundedCoord>(Middle.ClickableCoords); // middle and vertices

                foreach (Line line in Lines) {
                    coords.Add(line.Middle.CenterCoord);
                }

                return coords.ToArray();
            }
        }

        public override BaseBoundedCoord GetNextSetupCoord(System.Windows.Point? lastMouseDown) {
            if (!(lastMouseDown?.IsWithinRadiusOf(Vertices.First().AsPoint, ClickableCoordRadius) ?? false)) {
                BoundedCoord newCoord = new BoundedCoord(this, -2, (0, ContainerSize.Item1), -2, (0, ContainerSize.Item2));

                Vertices.Add(newCoord);
                (Middle.CoordController.X as NamedBoundedValueController).ControlledValues.Add(newCoord.X as NamedBoundedValue);
                (Middle.CoordController.Y as NamedBoundedValueController).ControlledValues.Add(newCoord.Y as NamedBoundedValue);
                CoordSetupQueue.Enqueue(newCoord);
            }

            return base.GetNextSetupCoord(lastMouseDown);
        }
        #endregion

        #region creation
        public Polygon(IRefreshableContainer refreshableContainer, DataContractSerializer serializer, VertexPointController vertexPointController, string name = nameof(Polygon))
            : base(refreshableContainer, serializer, name) {
            Color = vertexPointController.Color;

            HashSet<BoundedCoord> copiedVerts = new HashSet<BoundedCoord>();
            foreach (BoundedCoord coord in vertexPointController.CoordController.ControlledCoords) {
                copiedVerts.Add((BoundedCoord)coord.Clone());
            }
            Middle = new VertexPointController(this, null, Color, "Middle", copiedVerts);

            foreach (BoundedCoord vertex in copiedVerts) {
                CoordSetupQueue.Enqueue(vertex);
            }
        }

        public Polygon(Polygon polygon)
            : this(polygon.RefreshableContainer,
                  polygon.ShapeSerializer,
                  polygon.Middle,
                  polygon.BaseName) {
            RenderSettingsProvider = polygon.RenderSettingsProvider;
        }

        public override object Clone()
            => new Polygon(this);
        #endregion
    }
}
