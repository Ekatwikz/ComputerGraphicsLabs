using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows.Data;

namespace WPFDrawing {
    [DataContract]
    [KnownType(typeof(NamedBoundedValue))]
    [KnownType(typeof(NamedBoundedValueController))]
    public abstract class BaseNamedBoundedValue : NamedMemberOfRefreshable, ICloneable { // TODO: generic?
        public string VerboseName { get => $"{BaseName}: {Math.Round(Value, 3)}"; }
        protected sealed override void BaseNameChangedHook() => OnPropertyChanged(nameof(VerboseName));

        [DataMember]
        public double LowerBound { get; set; }

        [DataMember]
        public double UpperBound { get; set; }

        [DataMember]
        public abstract double Value { get; set; }

        public abstract object Clone();
    }

    [ValueConversion(typeof(BaseNamedBoundedValue), typeof(double))]
    internal class TickFrequencyConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is BaseNamedBoundedValue param) {
                return (param.UpperBound - param.LowerBound) / 10D;
            }
            return -1; // ??
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
