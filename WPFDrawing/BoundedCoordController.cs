using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WPFDrawing {
    [DataContract]
    public class BoundedCoordController : BaseBoundedCoord {
        [DataMember]
        public HashSet<BoundedCoord> ControlledCoords { get; private set; } = new HashSet<BoundedCoord>();

        #region creation
        public BoundedCoordController(IRefreshableContainer refreshableContainer,
            string baseName,
            HashSet<BoundedCoord> controlledCoords,
            MoveDirection moveDirection) {
            Direction = moveDirection;

            RefreshableContainer = refreshableContainer;
            BaseName = baseName;

            ControlledCoords = new HashSet<BoundedCoord>(controlledCoords); // again, is a new container with the same references.
            var xValues = new NamedBoundedValueController(this, nameof(X));
            var yValues = new NamedBoundedValueController(this, nameof(Y));
            foreach (BoundedCoord coord in ControlledCoords) { // ... ?
                coord.RefreshableContainer = this; // yuck.

                xValues.ControlledValues.Add(coord.X as NamedBoundedValue);
                yValues.ControlledValues.Add(coord.Y as NamedBoundedValue);
            }

            X = xValues;
            Y = yValues;
        }

        public BoundedCoordController(IRefreshableContainer refreshableContainer,
            string baseName,
            MoveDirection moveDirection,
            params BoundedCoord[] controlledCoords)
            : this(refreshableContainer,
                  baseName,
                  new HashSet<BoundedCoord>(controlledCoords), moveDirection) { }

        public BoundedCoordController(BoundedCoordController boundedCoord)
            : this(boundedCoord.RefreshableContainer,
                  boundedCoord.BaseName,
                  boundedCoord.ControlledCoords, boundedCoord.Direction) { }

        public override object Clone() // this is most likely shallow!
            => new BoundedCoordController(this);
        #endregion
    }
}
