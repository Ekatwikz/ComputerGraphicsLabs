using System;
using System.Collections.ObjectModel;
using System.Text;

namespace Task1Filters {
    public class ByteFunctionDisplay : NamedMemberOfRefreshable, ICloneable, IRefreshableContainer {
        public void Refresh(bool forceRefresh = false) {
            OnPropertyChanged(nameof(Info));

            if (ByteFunction == null) {
                return;
            }

            // recalculate lookup table of byte hook
            for (int i = 0; i < 256; ++i) {
                LookupTable[i] = ByteFunction((byte)i, Parameters).ClipToByte();
            }

            RefreshableContainer?.Refresh();
        }

        #region stuffs
        public byte[] LookupTable { get; }

        private ObservableCollection<NamedBoundedValue> _parameters = new ObservableCollection<NamedBoundedValue>();
        public ObservableCollection<NamedBoundedValue> Parameters {
            get => _parameters;
            protected set {
                _parameters = new ObservableCollection<NamedBoundedValue>();

                foreach (var param in value) {
                    var clonedParam = param.Clone() as NamedBoundedValue;
                    clonedParam.RefreshableContainer = this;
                    _parameters.Add(clonedParam);
                }

                Refresh();
            }
        }

        private Func<byte, Collection<NamedBoundedValue>, double> _byteFunction;
        public Func<byte, Collection<NamedBoundedValue>, double> ByteFunction {
            get => _byteFunction;
            protected set {
                _byteFunction = value;
                Refresh();
            }
        }

        public string Info {
            get {
                StringBuilder stringBuilder = new StringBuilder();
                if (Parameters.Count > 0) {
                    for (int i = 0; i < Parameters.Count; ++i) {
                        stringBuilder.Append(Parameters[i].VerboseName);

                        if (i < Parameters.Count - 1) {
                            stringBuilder.Append(", ");
                        }
                    }
                }

                return stringBuilder.ToString();
            }
        }
        #endregion

        #region creation
        protected ByteFunctionDisplay() {
            BaseName = "Byte Function"; // ??
            LookupTable = new byte[256];
        }

        protected ByteFunctionDisplay(ObservableCollection<NamedBoundedValue> parameters)
            : this() {
            Parameters = parameters;
        }

        public ByteFunctionDisplay(ObservableCollection<NamedBoundedValue> parameters, Func<byte, Collection<NamedBoundedValue>, double> byteFunction)
            : this(parameters) {
            ByteFunction = byteFunction;
        }

        public ByteFunctionDisplay(IRefreshableContainer refreshableContainer, ObservableCollection<NamedBoundedValue> parameters, Func<byte, Collection<NamedBoundedValue>, double> byteFunction)
            : this(parameters, byteFunction) {
            RefreshableContainer = refreshableContainer;
        }

        public ByteFunctionDisplay(IRefreshableContainer refreshableContainer, Func<byte, Collection<NamedBoundedValue>, double> byteFunction, params NamedBoundedValue[] parameters)
            : this(refreshableContainer, new ObservableCollection<NamedBoundedValue>(parameters), byteFunction) { } 

        public ByteFunctionDisplay(ByteFunctionDisplay byteFunctionDisplay)
            : this(byteFunctionDisplay.RefreshableContainer, byteFunctionDisplay.Parameters, byteFunctionDisplay.ByteFunction) { }

        public object Clone()
            => new ByteFunctionDisplay(this);
        #endregion
    }
}
