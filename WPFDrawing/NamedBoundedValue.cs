using System.Runtime.Serialization;

// TODO: split this into a few different files
namespace WPFDrawing {
    [DataContract]
    public class NamedBoundedValue : BaseNamedBoundedValue {
        private double value_;
        public override double Value {
            get => value_;
            set {
                value_ = value;
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(VerboseName));

                RefreshableContainer?.Refresh();
            }
        }

        #region creation
        public NamedBoundedValue(string baseName, double value, (double, double) bounds) {
            BaseName = baseName;
            LowerBound = bounds.Item1;
            UpperBound = bounds.Item2;
            Value = value;
        }

        public NamedBoundedValue(IRefreshableContainer refreshableContainer, string baseName, double value, (double, double) bounds)
            : this(baseName, value, bounds) {
            RefreshableContainer = refreshableContainer;
        }

        public NamedBoundedValue(NamedBoundedValue namedBoundedValue)
            : this(namedBoundedValue.RefreshableContainer,
                  namedBoundedValue.BaseName,
                  namedBoundedValue.Value,
                  (namedBoundedValue.LowerBound, namedBoundedValue.UpperBound)
                  ) { }

        public override object Clone()
            => new NamedBoundedValue(this);
        #endregion
    }
}
