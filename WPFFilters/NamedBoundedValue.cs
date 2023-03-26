using System;
using System.Globalization;
using System.Windows.Data;

namespace WPFFilters {
    public class NamedBoundedValue : NamedMemberOfRefreshable, ICloneable {
        public string VerboseName { get => $"{BaseName}: {Math.Round(Value, 3)}"; }
        protected override void BaseNameChangedHook() => OnPropertyChanged(nameof(VerboseName));

        public double LowerBound { get; set; }
        public double UpperBound { get; set; }

        private double value_;
        public double Value {
            get => value_;
            set {
                if (LowerBound <= value && value <= UpperBound) {
                    value_ = value;
                    OnPropertyChanged(nameof(Value));
                    OnPropertyChanged(nameof(VerboseName));

                    RefreshableContainer?.Refresh();
                }
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

        public virtual object Clone()
            => new NamedBoundedValue(this);
        #endregion
    }

    [ValueConversion(typeof(NamedBoundedValue), typeof(double))]
    internal class TickFrequencyConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is NamedBoundedValue param) {
                return (param.UpperBound - param.LowerBound) / 10D;
            }
            return -1; // ?
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
