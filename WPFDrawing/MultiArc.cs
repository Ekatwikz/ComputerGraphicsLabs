using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WPFDrawing {
    [DataContract]
    public class MultiArc : Shape {
        [DataMember]
        public Line MainPart;

        public override RGBCoord[] PixelCoords {
            get {
                List<RGBCoord> pixels = new List<RGBCoord>(MainPart.PixelCoords);

                foreach (Circle circle in Circles) {
                    foreach (var newPix in circle.PixelCoords) {
                        pixels.Add(newPix);
                    }
                }

                return pixels.ToArray();
            }
        }

        public List<Circle> Circles {
            get {
                int N = 3;
                List<Circle> circles = new List<Circle>();

                for (int i = 0; i < N; ++i) {
                    BoundedCoord edgePoint1 = new BoundedCoord(this,
                        MainPart.Start.AsPoint.X + (MainPart.End.X.Value - MainPart.Start.X.Value) * i / N,
                        (0, Bounds.Item1),
                        MainPart.Start.AsPoint.Y + (MainPart.End.Y.Value - MainPart.Start.Y.Value) * i / N,
                        (0, Bounds.Item2));
                    BoundedCoord edgePoint2 = new BoundedCoord(this,
                        MainPart.Start.AsPoint.X + (MainPart.End.X.Value - MainPart.Start.X.Value) * (i + 1) / N,
                        (0, Bounds.Item1),
                        MainPart.Start.AsPoint.Y + (MainPart.End.Y.Value - MainPart.Start.Y.Value) * (i + 1) / N,
                        (0, Bounds.Item2));

                    var circ = new Circle(this, null, Color,
                        new Line(this, null, Color, MoveDirection.BOTH, new VertexPoint(edgePoint1, false), new VertexPoint(edgePoint2, false))) {
                        RenderSettingsProvider = RenderSettingsProvider,
                    };

                    circles.Add(circ);
                }

                return circles;
            }
        }

        public override string VerboseName => $"{BaseName}??";

        public override BaseBoundedCoord[] ClickableCoords => MainPart.ClickableCoords;

        #region Creation
        public MultiArc(IRefreshableContainer refreshableContainer, Line line, IRenderSettingsProvider renderSettingsProvider)
            : base(refreshableContainer, null, nameof(MultiArc)) {
            RenderSettingsProvider = renderSettingsProvider;
            Color = new SelectableColor("Magenta");
            MainPart = (Line)line.Clone();
            MainPart.RefreshableContainer = this;
            MainPart.RenderSettingsProvider = RenderSettingsProvider;
            CoordSetupQueue.Enqueue(MainPart.Start);
            CoordSetupQueue.Enqueue(MainPart.End);
        }

        public MultiArc(MultiArc multiCircle)
            : this(multiCircle.RefreshableContainer, multiCircle.MainPart, multiCircle.RenderSettingsProvider) { }

        public override object Clone()
            => new MultiArc(this);
        #endregion
    }
}
