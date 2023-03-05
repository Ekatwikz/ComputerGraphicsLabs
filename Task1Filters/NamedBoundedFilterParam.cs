using System;
using System.ComponentModel;

namespace Task1Filters {
    public class NamedBoundedFilterParam : INotifyPropertyChanged, ICloneable {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string BaseName { get; set; }
        public string VerboseName { get => $"{BaseName}: {Math.Round(Value, 3)}"; }

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
                }
            }
        }

        public NamedBoundedFilterParam(string baseName, double value, (double, double) bounds) {
            BaseName = baseName;
            LowerBound = bounds.Item1;
            UpperBound = bounds.Item2;
            Value = value;
        }

        public object Clone() {
            return MemberwiseClone();
        }
    }
}
