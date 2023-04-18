using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WPFDrawing {
    [DataContract]
    public class VertexPoint : BasePoint { // TODO: edge version, rename this one
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

                return pixels.ToArray();
            }
        }

        public override BaseBoundedCoord[] ClickableCoords => new BaseBoundedCoord[] { CenterCoord };

        #region creation
        private VertexPoint(IRefreshableContainer refreshableContainer,
            DataContractSerializer shapeSerializer,
            (double, double) coords,
            SelectableColor defaultColor,
            string baseName = "VertexPoint")
            : base(refreshableContainer, shapeSerializer, defaultColor, baseName) {
            CenterCoord = new BoundedCoord(this,
                coords.Item1, (0, ContainerSize.Item1),
                coords.Item2, (0, ContainerSize.Item2), "Vertex");
            CoordSetupQueue.Enqueue(CenterCoord);
        }

        public VertexPoint(BaseBoundedCoord coords, bool shouldClone = true) // :(
            : base(coords.RefreshableContainer, null, new SelectableColor("Black"), "Vertex") { // gross
            CenterCoord = shouldClone ? (coords.Clone() as BaseBoundedCoord) : coords;
            CoordSetupQueue.Enqueue(CenterCoord);
        }

        public VertexPoint(IRefreshableContainer refreshableContainer, DataContractSerializer shapeSerializer)
            : this(refreshableContainer, shapeSerializer, (-2, -2), new SelectableColor("Red")) { Console.WriteLine("NO SETTINGS PROVIDED?!"); } // TMP!!

        public VertexPoint(VertexPoint point)
            : this(point.RefreshableContainer,
                  point.ShapeSerializer,
                  (point.CenterCoord.X.Value, point.CenterCoord.Y.Value),
                  point.Color,
                  point.BaseName) { }

        public override object Clone()
            => new VertexPoint(this);
        #endregion
    }
}
