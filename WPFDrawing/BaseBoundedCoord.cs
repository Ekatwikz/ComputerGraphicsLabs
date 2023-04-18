using System;
using System.Runtime.Serialization;

namespace WPFDrawing {
    [DataContract]
    [KnownType(typeof(BoundedCoord))]
    [KnownType(typeof(BoundedCoordController))]
    public abstract class BaseBoundedCoord : NamedMemberOfRefreshable, ICloneable, IBoundedRefreshableContainer {
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
                // boomerang refresh... ?
                X.Value = value.X;
                Y.Value = value.Y;
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
