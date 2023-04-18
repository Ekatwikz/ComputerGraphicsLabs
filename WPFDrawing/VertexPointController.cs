using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WPFDrawing {
    [DataContract]
    public class VertexPointController : BasePoint {
        public override RGBCoord[] PixelCoords {
            get {
                List<RGBCoord> pixels = new List<RGBCoord>(9);

                for (int i = 0; i < 9; ++i) {
                    int x = i % 3 - 1;
                    int y = i / 3 - 1;
                    pixels.Add(new RGBCoord() {
                        Coord = ((int)CenterCoord.X.Value + x, (int)CenterCoord.Y.Value + y),
                        CoordColor = Color.SelectedColor
                    });
                }

                foreach (BaseBoundedCoord coord in CoordController.ControlledCoords) {
                    for (int i = 0; i < 9; ++i) {
                        int x = i % 3 - 1;
                        int y = i / 3 - 1;
                        pixels.Add(new RGBCoord() {
                            Coord = ((int)coord.X.Value + x, (int)coord.Y.Value + y),
                            CoordColor = Color.SelectedColor.Invert()
                        });
                    }
                }

                return pixels.ToArray();
            }
        }

        #region stuff
        public BoundedCoordController CoordController => CenterCoord as BoundedCoordController;
        public override void RefreshHook() {
            // ehh??
        }

        public override BaseBoundedCoord[] ClickableCoords {
            get {
                List<BaseBoundedCoord> clickables = new List<BaseBoundedCoord>() { CenterCoord };

                foreach (BaseBoundedCoord coord in CoordController.ControlledCoords) {
                    clickables.Add(coord);
                }

                return clickables.ToArray();
            }
        }
        #endregion

        #region creation
        public VertexPointController(IRefreshableContainer refreshableContainer,
            DataContractSerializer shapeSerializer,
            SelectableColor defaultColor,
            string baseName,
            HashSet<BoundedCoord> coordSet)
            : base(refreshableContainer, shapeSerializer, defaultColor, baseName) {
            foreach (BoundedCoord coord in coordSet) {
                CoordSetupQueue.Enqueue(coord);
            }

            CenterCoord = new BoundedCoordController(this, "CCenter", coordSet);
        }

        // repitition ughh...
        public VertexPointController(IRefreshableContainer refreshableContainer,
            DataContractSerializer shapeSerializer,
            SelectableColor defaultColor,
            string baseName,
            params VertexPoint[] points)
            : base(refreshableContainer, shapeSerializer, defaultColor, baseName) {
            HashSet<BoundedCoord> coordSet = new HashSet<BoundedCoord>();

            foreach (VertexPoint point in points) {
                coordSet.Add(point.CenterCoord as BoundedCoord); // ?
                point.RefreshableContainer = this;
            }

            foreach (BoundedCoord coord in coordSet) {
                CoordSetupQueue.Enqueue(coord);
            }

            CenterCoord = new BoundedCoordController(this, "CCenter", coordSet);
        }

        // TMP:
        public VertexPointController(IRefreshableContainer refreshableContainer,
            DataContractSerializer shapeSerializer, params VertexPoint[] points)
            : this(refreshableContainer, shapeSerializer, new SelectableColor("Blue"), "Vertex Controller", points) { }

        VertexPointController(VertexPointController vertexPointController)
            : this(vertexPointController.RefreshableContainer,
                  vertexPointController.ShapeSerializer,
                  vertexPointController.Color,
                  vertexPointController.BaseName,
                  vertexPointController.CoordController.ControlledCoords) { }

        public override object Clone()
            => new VertexPointController(this);
        #endregion
    }
}