using System;
using System.Runtime.Serialization;

namespace WPFDrawing {
    public enum MoveDirection {
        VERTICAL = 1,
        HORIZONTAL,
        BOTH
    }

    [DataContract]
    [KnownType(typeof(BoundedCoord))]
    [KnownType(typeof(BoundedCoordController))]
    public abstract class BaseBoundedCoord : NamedMemberOfRefreshable, ICloneable, IBoundedRefreshableContainer {
        public MoveDirection Direction { get; set; } = MoveDirection.BOTH;

        [DataMember]
        public BaseNamedBoundedValue X { get; set; }

        [DataMember]
        public BaseNamedBoundedValue Y { get; set; }

        public System.Windows.Point AsPoint {
            get => new System.Windows.Point {
                X = X.Value,
                Y = Y.Value
            };

            set {
                if (Direction == 0) {
                    Console.WriteLine("DIRECTION NOT SET?!?!!");
                    Direction = MoveDirection.BOTH;
                }

                if ((Direction & MoveDirection.HORIZONTAL) > 0) {
                    X.Value = value.X;
                }

                if ((Direction & MoveDirection.VERTICAL) > 0) {
                    Y.Value = value.Y;
                }

                // boomerang refresh... ?
                Refresh();
            }
        }

        public string VerboseName => $"{BaseName}: (X: {Math.Round(X.Value, 3)}, Y: {Math.Round(Y.Value, 3)})";

        public (int, int) Bounds => ((int)X.UpperBound, (int)Y.UpperBound); // vomit emoji

        public void Refresh(bool forceRefresh = false) {
            RefreshableContainer?.Refresh(forceRefresh);
            OnPropertyChanged(nameof(VerboseName));
        }

        public abstract object Clone();
    }
}
