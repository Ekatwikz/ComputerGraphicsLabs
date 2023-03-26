using System.Windows.Input;

namespace WPFFilters {
    public class ResettableNamedBoundedValue : NamedBoundedValue {
        public double DefaultValue { get; }

        public ICommand ResetValueCommand { get; private set; }
        public void ResetValue() {
            Value = DefaultValue;
            OnPropertyChanged(nameof(VerboseName));
            OnPropertyChanged(nameof(Value)); // ?
        }

        #region creation
        protected ResettableNamedBoundedValue(string baseName, double value, (double, double) bounds, double defaultValue = 0)
            : base(baseName, value, bounds) {
            ResetValueCommand = new RelayCommand(ResetValue);
            DefaultValue = defaultValue;
        }

        public ResettableNamedBoundedValue(IRefreshableContainer refreshableContainer, string baseName, double value, (double, double) bounds, double defaultValue = 0)
            : this(baseName, value, bounds, defaultValue) {
            RefreshableContainer = refreshableContainer;
        }

        public ResettableNamedBoundedValue(ResettableNamedBoundedValue resettableNamedBoundedValue)
            : this(resettableNamedBoundedValue.RefreshableContainer, resettableNamedBoundedValue.BaseName, resettableNamedBoundedValue.Value, (resettableNamedBoundedValue.LowerBound, resettableNamedBoundedValue.UpperBound)) { }

        public override object Clone()
            => new ResettableNamedBoundedValue(this);
        #endregion
    }
}
