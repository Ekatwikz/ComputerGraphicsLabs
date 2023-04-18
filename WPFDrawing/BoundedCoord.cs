using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WPFDrawing {
    [DataContract]
    public class BoundedCoord : BaseBoundedCoord {
        #region creation
        public BoundedCoord(IRefreshableContainer refreshableContainer,
            double x,
            (double, double) xBounds,
            double y,
            (double, double) yBounds,
            string baseName = "Coord") {
            RefreshableContainer = refreshableContainer;
            BaseName = baseName;
            X = new NamedBoundedValue(this, "X", x, xBounds);
            Y = new NamedBoundedValue(this, "Y", y, yBounds);
        }

        public BoundedCoord(BoundedCoord boundedCoord)
            : this(boundedCoord.RefreshableContainer, boundedCoord.X.Value, (boundedCoord.X.LowerBound, boundedCoord.X.UpperBound),
                  boundedCoord.Y.Value, (boundedCoord.Y.LowerBound, boundedCoord.Y.UpperBound),
                  boundedCoord.BaseName) { }

        public override object Clone()
            => new BoundedCoord(this);
        #endregion
    }
}
